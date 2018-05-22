using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DynamicPanels
{
	[DisallowMultipleComponent]
	public class DynamicPanelsCanvas : MonoBehaviour, IPointerEnterHandler, ISerializationCallbackReceiver
	{
		public class InternalSettings
		{
			private readonly DynamicPanelsCanvas canvas;
			public readonly Camera worldCamera;

			public InternalSettings( DynamicPanelsCanvas canvas )
			{
				this.canvas = canvas;

#if UNITY_EDITOR
				if( canvas.UnityCanvas == null ) // is null while inspecting this component in edit mode
					return;
#endif

				if( canvas.UnityCanvas.renderMode == RenderMode.ScreenSpaceOverlay || 
					( canvas.UnityCanvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.UnityCanvas.worldCamera == null ) )
					worldCamera = null;
				else
					worldCamera = canvas.UnityCanvas.worldCamera ?? Camera.main;
			}

			public List<PanelProperties> InitialPanelsUnanchored
			{
				get
				{
					if( canvas.initialPanelsUnanchored == null )
						canvas.initialPanelsUnanchored = new List<PanelProperties>();

					return canvas.initialPanelsUnanchored;
				}
			}

			public AnchoredPanelProperties InitialPanelsAnchored
			{
				get
				{
					if( canvas.initialPanelsAnchored == null )
						canvas.initialPanelsAnchored = new AnchoredPanelProperties();

					return canvas.initialPanelsAnchored;
				}
			}

			public void AnchorZonesSetActive( bool value ) { canvas.AnchorZonesSetActive( value ); }
			public void ReceiveRaycasts( bool value ) { canvas.background.raycastTarget = value; }
			public void OnApplicationQuit() { canvas.OnApplicationQuit(); }
		}

		[System.Serializable]
		public class PanelProperties
		{
			public List<PanelTabProperties> tabs = new List<PanelTabProperties>();
		}

		public class AnchoredPanelProperties
		{
			public PanelProperties panel = new PanelProperties();
			public Direction anchorDirection;

			public List<AnchoredPanelProperties> subPanels = new List<AnchoredPanelProperties>();
		}

		// Credit: https://docs.unity3d.com/Manual/script-Serialization-Custom.html
		[System.Serializable]
		public struct SerializableAnchoredPanelProperties
		{
			public PanelProperties panel;
			public Direction anchorDirection;

			public int childCount;
			public int indexOfFirstChild;
		}

		[System.Serializable]
		public class PanelTabProperties
		{
			public RectTransform content = null;
			public Vector2 minimumSize = new Vector2( 250f, 300f );

			public string tabLabel = "Panel";
			public Sprite tabIcon = null;
		}

		public RectTransform RectTransform { get; private set; }
		public Canvas UnityCanvas { get; private set; }

#if UNITY_EDITOR
		private InternalSettings m_internal;
		public InternalSettings Internal
		{
			get
			{
				if( m_internal == null )
					m_internal = new InternalSettings( this );

				return m_internal;
			}
		}
#else
		public InternalSettings Internal { get; private set; }
#endif

		public UnanchoredPanelGroup UnanchoredPanelGroup { get; private set; }
		public PanelGroup RootPanelGroup { get; private set; }
		
		public Vector2 Size { get; private set; }

		private Panel dummyPanel;
		private Image background;

		private RectTransform anchorZonesParent;
		private readonly CanvasAnchorZone[] anchorZones = new CanvasAnchorZone[4]; // one for each side

		[SerializeField]
		private Vector2 minimumFreeSpace = new Vector2( 50f, 50f );
		
		[SerializeField]
		private float m_panelResizableAreaLength = 12f;
		public float PanelResizableAreaLength { get { return m_panelResizableAreaLength; } }

		[SerializeField]
		private float m_canvasAnchorZoneLength = 20f;
		public float CanvasAnchorZoneLength { get { return m_canvasAnchorZoneLength; } }

		[SerializeField]
		private float m_panelAnchorZoneLength = 100f;
		public float PanelAnchorZoneLength { get { return m_panelAnchorZoneLength; } }
		
		private const float m_panelAnchorZoneLengthRatio = 0.31f;
		public float PanelAnchorZoneLengthRatio { get { return m_panelAnchorZoneLengthRatio; } }

		[SerializeField]
		private List<PanelProperties> initialPanelsUnanchored;
		
		[SerializeField]
		[HideInInspector]
		private List<SerializableAnchoredPanelProperties> initialPanelsAnchoredSerialized;
		private AnchoredPanelProperties initialPanelsAnchored;
		
		private bool updateBounds = true;
		private bool isDirty = false;

		private bool isQuitting = false;

		private void Awake()
		{
			RectTransform = (RectTransform) transform;
			UnityCanvas = GetComponentInParent<Canvas>();
#if !UNITY_EDITOR
			Internal = new InternalSettings( this );
#endif

			UnanchoredPanelGroup = new UnanchoredPanelGroup( this );
			RectTransform.pivot = new Vector2( 0.5f, 0.5f );
			
			if( GetComponent<RectMask2D>() == null )
				gameObject.AddComponent<RectMask2D>();

			Size = RectTransform.rect.size;

			InitializeRootGroup();
			InitializeAnchorZones();

			background = GetComponent<Image>();
			if( background == null )
			{
				background = gameObject.AddComponent<Image>();
				background.sprite = dummyPanel.Internal.BackgroundSprite;
				background.color = Color.clear;
				background.raycastTarget = false;
			}

			PanelManager.Instance.RegisterCanvas( this );
		}

		private void Start()
		{
			Size = RectTransform.rect.size;

			HashSet<Transform> createdTabs = new HashSet<Transform>(); // A set to prevent duplicate tabs or to prevent making canvas itself a panel
			Transform tr = transform;
			while( tr != null )
			{
				createdTabs.Add( tr );
				tr = tr.parent;
			}

			if( initialPanelsAnchored != null )
				CreateAnchoredPanelsRecursively( initialPanelsAnchored.subPanels, dummyPanel, createdTabs );

			for( int i = 0; i < initialPanelsUnanchored.Count; i++ )
				CreateInitialPanel( initialPanelsUnanchored[i], null, Direction.None, createdTabs );
			
			initialPanelsUnanchored = null;
			initialPanelsAnchored = null;
			initialPanelsAnchoredSerialized = null;

			LateUpdate(); // update layout

			RootPanelGroup.Internal.TryChangeSizeOf( dummyPanel, Direction.Left, 20009f ); // Minimize all panels to their minimum size
			RootPanelGroup.Internal.TryChangeSizeOf( dummyPanel, Direction.Top, 20009f ); // Magick number..
			RootPanelGroup.Internal.TryChangeSizeOf( dummyPanel, Direction.Right, 20009f ); // or not?
			RootPanelGroup.Internal.TryChangeSizeOf( dummyPanel, Direction.Bottom, 20009f ); // only time will tell _/o_o\_
		}

		private void OnDestroy()
		{
			if( !isQuitting )
				PanelManager.Instance.UnregisterCanvas( this );
		}

		private void OnApplicationQuit()
		{
			isQuitting = true;
		}

		private void LateUpdate()
		{
			if( isDirty )
			{
				PanelManager.Instance.StopCanvasOperations( this );

				RootPanelGroup.Internal.UpdateLayout();
				UnanchoredPanelGroup.Internal.UpdateLayout();

				RootPanelGroup.Internal.UpdateSurroundings( null, null, null, null );
			}

			if( updateBounds )
			{
				UpdateBounds();
				updateBounds = false;
			}

			if( isDirty )
			{
				RootPanelGroup.Internal.EnsureMinimumSize();
				UnanchoredPanelGroup.Internal.EnsureMinimumSize();

				isDirty = false;
			}
		}

		public void SetDirty()
		{
			isDirty = true;
			updateBounds = true;
		}

		public void ForceRebuildLayoutImmediate()
		{
			LateUpdate();
		}

		public void OnPointerEnter( PointerEventData eventData )
		{
			PanelManager.Instance.OnPointerEnteredCanvas( this, eventData );
		}

		private void OnRectTransformDimensionsChange()
		{
			updateBounds = true;
		}
		
		private void UpdateBounds()
		{
			Size = RectTransform.rect.size;

			RootPanelGroup.Internal.UpdateBounds( Vector2.zero, Size );
			UnanchoredPanelGroup.Internal.UpdateBounds( Vector2.zero, Size );
		}

		private void CreateAnchoredPanelsRecursively( List<AnchoredPanelProperties> anchoredPanels, Panel rootPanel, HashSet<Transform> createdTabs )
		{
			if( anchoredPanels == null )
				return;

			for( int i = 0; i < anchoredPanels.Count; i++ )
			{
				Panel panel = CreateInitialPanel( anchoredPanels[i].panel, rootPanel, anchoredPanels[i].anchorDirection, createdTabs );
				if( panel == null )
					panel = rootPanel;

				CreateAnchoredPanelsRecursively( anchoredPanels[i].subPanels, panel, createdTabs );
			}
		}

		private Panel CreateInitialPanel( PanelProperties properties, Panel anchor, Direction anchorDirection, HashSet<Transform> createdTabs )
		{
			Panel panel = null;
			for( int i = 0; i < properties.tabs.Count; i++ )
			{
				PanelTabProperties panelProps = properties.tabs[i];
				if( panelProps.content != null && !panelProps.content.Equals( null ) )
				{
					if( createdTabs.Contains( panelProps.content ) )
						continue;

					if( panelProps.content.parent != RectTransform )
						panelProps.content.SetParent( RectTransform, false );

					int tabIndex = 0;
					if( panel == null )
						panel = PanelUtils.CreatePanelFor( panelProps.content, this );
					else
						tabIndex = panel.AddTab( panelProps.content );
					
					panel.SetTabTitle( tabIndex, panelProps.tabIcon, panelProps.tabLabel );
					panel.SetTabMinSize( tabIndex, panelProps.minimumSize );

					createdTabs.Add( panelProps.content );
				}
			}

			if( panel != null )
			{
				panel.ActiveTab = 0;

				if( anchor != null && anchorDirection != Direction.None )
					panel.DockToPanel( anchor, anchorDirection );
			}

			return panel;
		}

		private void InitializeRootGroup()
		{
			dummyPanel = PanelUtils.Internal.CreatePanel( null, this );
			dummyPanel.gameObject.name = "DummyPanel";
			dummyPanel.CanvasGroup.alpha = 0f;
			dummyPanel.Internal.SetDummy( minimumFreeSpace );

			RootPanelGroup = new PanelGroup( this, Direction.Right );
			RootPanelGroup.AddElement( dummyPanel );
		}

		private void InitializeAnchorZones()
		{
			anchorZonesParent = (RectTransform) new GameObject( "CanvasAnchorZone", typeof( RectTransform ) ).transform;
			anchorZonesParent.SetParent( RectTransform, false );
			anchorZonesParent.anchorMin = Vector2.zero;
			anchorZonesParent.anchorMax = Vector2.one;
			anchorZonesParent.sizeDelta = Vector2.zero;

			CreateAnchorZone( Direction.Left, new Vector2( 0f, 0f ), new Vector2( 0f, 1f ), new Vector2( m_canvasAnchorZoneLength, 0f ) );
			CreateAnchorZone( Direction.Top, new Vector2( 0f, 1f ), new Vector2( 1f, 1f ), new Vector2( 0f, m_canvasAnchorZoneLength ) );
			CreateAnchorZone( Direction.Right, new Vector2( 1f, 0f ), new Vector2( 1f, 1f ), new Vector2( m_canvasAnchorZoneLength, 0f ) );
			CreateAnchorZone( Direction.Bottom, new Vector2( 0f, 0f ), new Vector2( 1f, 0f ), new Vector2( 0f, m_canvasAnchorZoneLength ) );

			for( int i = 0; i < anchorZones.Length; i++ )
				anchorZones[i].SetActive( false );
		}

		private void CreateAnchorZone( Direction direction, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta )
		{
			CanvasAnchorZone anchorZone = new GameObject( "AnchorZone" + direction, typeof( RectTransform ) ).AddComponent<CanvasAnchorZone>();
			anchorZone.Initialize( dummyPanel );
			anchorZone.SetDirection( direction );

			anchorZone.RectTransform.SetParent( anchorZonesParent, false );

			anchorZone.RectTransform.pivot = anchorMin;
			anchorZone.RectTransform.anchorMin = anchorMin;
			anchorZone.RectTransform.anchorMax = anchorMax;
			anchorZone.RectTransform.anchoredPosition = Vector2.zero;
			anchorZone.RectTransform.sizeDelta = sizeDelta;

			anchorZones[(int) direction] = anchorZone;
		}

		private void AnchorZonesSetActive( bool value )
		{
			if( !enabled )
				return;

			if( value )
				anchorZonesParent.SetAsLastSibling();

			for( int i = 0; i < anchorZones.Length; i++ )
				anchorZones[i].SetActive( value );
		}

		public void OnBeforeSerialize()
		{
			if( initialPanelsAnchoredSerialized == null )
				initialPanelsAnchoredSerialized = new List<SerializableAnchoredPanelProperties>();
			else
				initialPanelsAnchoredSerialized.Clear();

			if( initialPanelsAnchored == null )
				initialPanelsAnchored = new AnchoredPanelProperties();

			AddToSerializedAnchoredPanelProperties( initialPanelsAnchored );
		}

		public void OnAfterDeserialize()
		{
			if( initialPanelsAnchoredSerialized != null && initialPanelsAnchoredSerialized.Count > 0 )
				ReadFromSerializedAnchoredPanelProperties( 0, out initialPanelsAnchored );
			else
				initialPanelsAnchored = new AnchoredPanelProperties();
		}

		private void AddToSerializedAnchoredPanelProperties( AnchoredPanelProperties props )
		{
			SerializableAnchoredPanelProperties serializedProps = new SerializableAnchoredPanelProperties()
			{
				panel = props.panel,
				anchorDirection = props.anchorDirection,
				childCount = props.subPanels.Count,
				indexOfFirstChild = initialPanelsAnchoredSerialized.Count + 1
			};

			initialPanelsAnchoredSerialized.Add( serializedProps );
			for( int i = 0; i < props.subPanels.Count; i++ )
				AddToSerializedAnchoredPanelProperties( props.subPanels[i] );
		}

		private int ReadFromSerializedAnchoredPanelProperties( int index, out AnchoredPanelProperties props )
		{
			SerializableAnchoredPanelProperties serializedProps = initialPanelsAnchoredSerialized[index];
			AnchoredPanelProperties newProps = new AnchoredPanelProperties()
			{
				panel = serializedProps.panel,
				anchorDirection = serializedProps.anchorDirection,
				subPanels = new List<AnchoredPanelProperties>()
			};
			
			for( int i = 0; i != serializedProps.childCount; i++ )
			{
				AnchoredPanelProperties childProps;
				index = ReadFromSerializedAnchoredPanelProperties( ++index, out childProps );
				newProps.subPanels.Add( childProps );
			}

			props = newProps;
			return index;
		}
	}
}