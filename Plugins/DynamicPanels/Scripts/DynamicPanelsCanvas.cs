using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_2017_3_OR_NEWER
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo( "DynamicPanels.Editor" )]
#else
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo( "Assembly-CSharp-Editor" )]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo( "Assembly-CSharp-Editor-firstpass" )]
#endif
namespace DynamicPanels
{
	[DisallowMultipleComponent]
	public class DynamicPanelsCanvas : MonoBehaviour, IPointerEnterHandler, ISerializationCallbackReceiver
	{
		internal class InternalSettings
		{
			private readonly DynamicPanelsCanvas canvas;
			public readonly Camera worldCamera;

			public InternalSettings( DynamicPanelsCanvas canvas )
			{
				this.canvas = canvas;

#if UNITY_EDITOR
				if( !canvas.UnityCanvas ) // is null while inspecting this component in edit mode
					return;
#endif

				if( canvas.UnityCanvas.renderMode == RenderMode.ScreenSpaceOverlay ||
					( canvas.UnityCanvas.renderMode == RenderMode.ScreenSpaceCamera && !canvas.UnityCanvas.worldCamera ) )
					worldCamera = null;
				else
					worldCamera = canvas.UnityCanvas.worldCamera ? canvas.UnityCanvas.worldCamera : Camera.main;
			}

			public Panel DummyPanel { get { return canvas.dummyPanel; } }

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

			public bool IsLastDockedPanel( Panel panel )
			{
				return panel.IsDocked && !PanelGroupHasAnyOtherPanels( canvas.RootPanelGroup, panel );
			}

			private bool PanelGroupHasAnyOtherPanels( PanelGroup group, Panel panel )
			{
				for( int i = 0; i < group.Count; i++ )
				{
					if( group[i] is Panel )
					{
						Panel _panel = (Panel) group[i];
						if( _panel != panel && _panel != canvas.dummyPanel )
							return true;
					}
					else if( PanelGroupHasAnyOtherPanels( (PanelGroup) group[i], panel ) )
						return true;
				}

				return false;
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
			public Vector2 initialSize;

			public List<AnchoredPanelProperties> subPanels = new List<AnchoredPanelProperties>();
		}

		// Credit: https://docs.unity3d.com/Manual/script-Serialization-Custom.html
		[System.Serializable]
		public struct SerializableAnchoredPanelProperties
		{
			public PanelProperties panel;
			public Direction anchorDirection;
			public Vector2 initialSize;

			public int childCount;
			public int indexOfFirstChild;
		}

		[System.Serializable]
		public class PanelTabProperties : ISerializationCallbackReceiver
		{
			public RectTransform content = null;
			public string id = null;
			public Vector2 minimumSize = new Vector2( 250f, 300f );

			public string tabLabel = "Panel";
			public Sprite tabIcon = null;

			void ISerializationCallbackReceiver.OnBeforeSerialize()
			{
				if( string.IsNullOrEmpty( id ) )
					id = System.Guid.NewGuid().ToString();
			}

			void ISerializationCallbackReceiver.OnAfterDeserialize()
			{
			}
		}

		public RectTransform RectTransform { get; private set; }
		public Canvas UnityCanvas { get; private set; }

#if UNITY_EDITOR
		private InternalSettings m_internal;
		internal InternalSettings Internal
		{
			get
			{
				if( m_internal == null )
					m_internal = new InternalSettings( this );

				return m_internal;
			}
		}
#else
		internal InternalSettings Internal { get; private set; }
#endif

		[SerializeField]
		[HideInInspector]
		private string m_id;
		public string ID
		{
			get { return m_id; }
			set { m_id = value; }
		}

		public UnanchoredPanelGroup UnanchoredPanelGroup { get; private set; }
		public PanelGroup RootPanelGroup { get; private set; }

		public Vector2 Size { get; private set; }

		private Panel dummyPanel;
		private Graphic background;

		private RectTransform anchorZonesParent;
		private readonly CanvasAnchorZone[] anchorZones = new CanvasAnchorZone[4]; // one for each side

#pragma warning disable 0649
		[SerializeField]
		private bool m_leaveFreeSpace = true;
		public bool LeaveFreeSpace
		{
			get { return m_leaveFreeSpace; }
			set
			{
				m_leaveFreeSpace = value;
				if( !m_leaveFreeSpace )
					dummyPanel.Detach();
				else if( !dummyPanel.IsDocked )
				{
					// Add the free space to the middle
					if( RootPanelGroup.Count <= 1 )
						RootPanelGroup.AddElement( dummyPanel );
					else
						RootPanelGroup.AddElementBefore( RootPanelGroup[RootPanelGroup.Count / 2], dummyPanel );
				}
			}
		}

		[SerializeField]
		private Vector2 minimumFreeSpace = new Vector2( 50f, 50f );

		[SerializeField]
		private RectTransform freeSpaceTargetTransform;
		private Vector2 freeSpacePrevPos, freeSpacePrevSize;

		public bool PreventDetachingLastDockedPanel;

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
#pragma warning restore 0649

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
			RectTransform.ChangePivotWithoutAffectingPosition( new Vector2( 0.5f, 0.5f ) );

			if( !GetComponent<RectMask2D>() )
				gameObject.AddComponent<RectMask2D>();

			Size = RectTransform.rect.size;

			InitializeRootGroup();
			InitializeAnchorZones();

			background = GetComponent<Graphic>();
			if( !background )
			{
				background = gameObject.AddComponent<NonDrawingGraphic>();
				background.raycastTarget = false;
			}

			PanelManager.Instance.RegisterCanvas( this );
		}

		private void Start()
		{
			Size = RectTransform.rect.size;

			HashSet<Transform> createdTabs = new HashSet<Transform>(); // A set to prevent duplicate tabs or to prevent making canvas itself a panel
			Transform tr = transform;
			while( tr )
			{
				createdTabs.Add( tr );
				tr = tr.parent;
			}

			Dictionary<Panel, Vector2> initialSizes = null;
			if( initialPanelsAnchored != null )
			{
				initialSizes = new Dictionary<Panel, Vector2>( initialPanelsAnchoredSerialized.Count );
				CreateAnchoredPanelsRecursively( initialPanelsAnchored.subPanels, dummyPanel, createdTabs, initialSizes );
			}

			for( int i = 0; i < initialPanelsUnanchored.Count; i++ )
				CreateInitialPanel( initialPanelsUnanchored[i], null, Direction.None, createdTabs );

			initialPanelsUnanchored = null;
			initialPanelsAnchored = null;
			initialPanelsAnchoredSerialized = null;

			if( freeSpaceTargetTransform )
			{
				if( freeSpaceTargetTransform.parent != RectTransform )
					freeSpaceTargetTransform.SetParent( RectTransform, false );

				freeSpaceTargetTransform.anchorMin = Vector2.zero;
				freeSpaceTargetTransform.anchorMax = Vector2.zero;
				freeSpaceTargetTransform.pivot = Vector2.zero;
				freeSpaceTargetTransform.SetAsFirstSibling();
			}

			LeaveFreeSpace = m_leaveFreeSpace;
			LateUpdate(); // Update layout

			if( m_leaveFreeSpace )
			{
				// Minimize all panels to their minimum size
				dummyPanel.ResizeTo( new Vector2( 99999f, 99999f ) );

				//RootPanelGroup.Internal.TryChangeSizeOf( dummyPanel, Direction.Left, 20009f );
				//RootPanelGroup.Internal.TryChangeSizeOf( dummyPanel, Direction.Top, 20009f ); // Magick number..
				//RootPanelGroup.Internal.TryChangeSizeOf( dummyPanel, Direction.Right, 20009f ); // or not?
				//RootPanelGroup.Internal.TryChangeSizeOf( dummyPanel, Direction.Bottom, 20009f ); // A: just a big random number U_U
			}

			if( initialSizes != null )
				ResizeAnchoredPanelsRecursively( RootPanelGroup, initialSizes );
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

			if( m_leaveFreeSpace && freeSpaceTargetTransform )
			{
				Vector2 freeSpacePos = dummyPanel.Position;
				Vector2 freeSpaceSize = dummyPanel.Size;
				if( freeSpacePos != freeSpacePrevPos || freeSpaceSize != freeSpacePrevSize )
				{
					freeSpacePrevPos = freeSpacePos;
					freeSpacePrevSize = freeSpaceSize;

					freeSpaceTargetTransform.anchoredPosition = freeSpacePos;
					freeSpaceTargetTransform.sizeDelta = freeSpaceSize;
				}
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

		void IPointerEnterHandler.OnPointerEnter( PointerEventData eventData )
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

		private void CreateAnchoredPanelsRecursively( List<AnchoredPanelProperties> anchoredPanels, Panel rootPanel, HashSet<Transform> createdTabs, Dictionary<Panel, Vector2> initialSizes )
		{
			if( anchoredPanels == null )
				return;

			for( int i = 0; i < anchoredPanels.Count; i++ )
			{
				Panel panel = CreateInitialPanel( anchoredPanels[i].panel, rootPanel, anchoredPanels[i].anchorDirection, createdTabs );
				if( panel == null )
					panel = rootPanel;
				else if( anchoredPanels[i].initialSize != Vector2.zero )
					initialSizes[panel] = anchoredPanels[i].initialSize;

				CreateAnchoredPanelsRecursively( anchoredPanels[i].subPanels, panel, createdTabs, initialSizes );
			}
		}

		private void ResizeAnchoredPanelsRecursively( PanelGroup group, Dictionary<Panel, Vector2> initialSizes )
		{
			if( group == null )
				return;

			int count = group.Count;
			for( int i = 0; i < count; i++ )
			{
				Panel panel = group[i] as Panel;
				if( panel != null )
				{
					Vector2 initialSize;
					if( initialSizes.TryGetValue( panel, out initialSize ) )
						panel.ResizeTo( initialSize, Direction.Right, Direction.Top );
				}
				else
					ResizeAnchoredPanelsRecursively( group[i] as PanelGroup, initialSizes );
			}
		}

		private Panel CreateInitialPanel( PanelProperties properties, Panel anchor, Direction anchorDirection, HashSet<Transform> createdTabs )
		{
			Panel panel = null;
			for( int i = 0; i < properties.tabs.Count; i++ )
			{
				PanelTabProperties panelProps = properties.tabs[i];
				if( panelProps.content )
				{
					if( createdTabs.Contains( panelProps.content ) )
						continue;

					if( panelProps.content.parent != RectTransform )
						panelProps.content.SetParent( RectTransform, false );

					PanelTab tab;
					if( panel == null )
					{
						panel = PanelUtils.CreatePanelFor( panelProps.content, this );
						tab = panel[0];
					}
					else
						tab = panel.AddTab( panelProps.content );

					tab.Icon = panelProps.tabIcon;
					tab.Label = panelProps.tabLabel;
					tab.MinSize = panelProps.minimumSize;
					tab.ID = panelProps.id;

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

		[ContextMenu( "Save Layout" )]
		public void SaveLayout()
		{
			PanelSerialization.SerializeCanvas( this );
		}

		[ContextMenu( "Load Layout" )]
		public void LoadLayout()
		{
			PanelSerialization.DeserializeCanvas( this );
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			if( initialPanelsAnchoredSerialized == null )
				initialPanelsAnchoredSerialized = new List<SerializableAnchoredPanelProperties>();
			else
				initialPanelsAnchoredSerialized.Clear();

			if( initialPanelsAnchored == null )
				initialPanelsAnchored = new AnchoredPanelProperties();

			if( string.IsNullOrEmpty( m_id ) )
				m_id = System.Guid.NewGuid().ToString();

			AddToSerializedAnchoredPanelProperties( initialPanelsAnchored );
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
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
				initialSize = props.initialSize,
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
				initialSize = serializedProps.initialSize,
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