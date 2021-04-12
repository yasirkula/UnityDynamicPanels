using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace DynamicPanels
{
	public enum Direction { None = -1, Left = 0, Top = 1, Right = 2, Bottom = 3 };

	[DisallowMultipleComponent]
	public class PanelManager : MonoBehaviour
	{
		public const int NON_EXISTING_TOUCH = -98765;
		private const float PANEL_TABS_VALIDATE_INTERVAL = 5f;

		private static PanelManager m_instance;
		public static PanelManager Instance
		{
			get
			{
				if( m_instance == null )
					m_instance = new GameObject( "PanelManager" ).AddComponent<PanelManager>();

				return m_instance;
			}
		}

		private List<DynamicPanelsCanvas> canvases = new List<DynamicPanelsCanvas>( 8 );
		private List<Panel> panels = new List<Panel>( 32 );

		private Panel draggedPanel = null;
		private AnchorZoneBase hoveredAnchorZone = null;

		private RectTransform previewPanel = null;
		private DynamicPanelsCanvas previewPanelCanvas;

		private float nextPanelValidationTime;
		private PointerEventData nullPointerEventData;

		private void Awake()
		{
			if( m_instance == null )
				m_instance = this;
			else if( this != m_instance )
			{
				Destroy( this );
				return;
			}

			InitializePreviewPanel();

			DontDestroyOnLoad( gameObject );
			DontDestroyOnLoad( previewPanel.gameObject );

			nextPanelValidationTime = Time.realtimeSinceStartup + PANEL_TABS_VALIDATE_INTERVAL;
			nullPointerEventData = new PointerEventData( null );
		}

		private void OnApplicationQuit()
		{
			for( int i = 0; i < panels.Count; i++ )
				panels[i].Internal.OnApplicationQuit();

			for( int i = 0; i < canvases.Count; i++ )
				canvases[i].Internal.OnApplicationQuit();
		}

		private void Update()
		{
			if( draggedPanel == null )
			{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
				if( Pointer.current != null )
				{
					if( Pointer.current.press.wasPressedThisFrame )
						BringClickedPanelToForward( Pointer.current.position.ReadValue() );
					else if( Mouse.current != null )
					{
						if( Mouse.current.rightButton.wasPressedThisFrame || Mouse.current.middleButton.wasPressedThisFrame )
							BringClickedPanelToForward( Mouse.current.position.ReadValue() );
					}
				}
#else
				if( Input.touchCount == 0 )
				{
					if( Input.GetMouseButtonDown( 0 ) || Input.GetMouseButtonDown( 1 ) || Input.GetMouseButtonDown( 2 ) )
						BringClickedPanelToForward( Input.mousePosition );
				}
				else
				{
					for( int i = 0; i < Input.touchCount; i++ )
					{
						Touch touch = Input.GetTouch( i );
						if( touch.phase == TouchPhase.Began )
							BringClickedPanelToForward( touch.position );
					}
				}
#endif
			}

			if( Time.realtimeSinceStartup >= nextPanelValidationTime )
			{
				for( int i = 0; i < panels.Count; i++ )
					panels[i].Internal.ValidateTabs();

				nextPanelValidationTime = Time.realtimeSinceStartup + PANEL_TABS_VALIDATE_INTERVAL;
			}
		}

		public void RegisterCanvas( DynamicPanelsCanvas canvas )
		{
			if( !canvases.Contains( canvas ) )
				canvases.Add( canvas );
		}

		public void UnregisterCanvas( DynamicPanelsCanvas canvas )
		{
			canvases.Remove( canvas );
			StopCanvasOperations( canvas );

			if( previewPanelCanvas == canvas )
				previewPanelCanvas = draggedPanel != null ? draggedPanel.Canvas : null;

			if( previewPanel.parent == canvas.RectTransform )
				previewPanel.SetParent( null, false );
		}

		public void RegisterPanel( Panel panel )
		{
			if( !panels.Contains( panel ) )
			{
				panels.Add( panel );
				panel.GetComponentInParent<DynamicPanelsCanvas>().UnanchoredPanelGroup.AddElement( panel );
			}
		}

		public void UnregisterPanel( Panel panel )
		{
			if( draggedPanel == panel )
				draggedPanel = null;

			panels.Remove( panel );
			panel.Group.Internal.SetDirty();
		}

		public void AnchorPanel( IPanelGroupElement source, DynamicPanelsCanvas canvas, Direction anchorDirection )
		{
			PanelGroup rootGroup = canvas.RootPanelGroup;
			PanelGroup tempGroup = new PanelGroup( canvas, Direction.Right );
			for( int i = 0; i < rootGroup.Count; i++ )
			{
				if( rootGroup[i].Group == rootGroup )
					tempGroup.AddElement( rootGroup[i] );
			}

			rootGroup.AddElement( tempGroup );
			AnchorPanel( source, tempGroup, anchorDirection );
		}

		public void AnchorPanel( IPanelGroupElement source, IPanelGroupElement anchor, Direction anchorDirection )
		{
			PanelGroup group = anchor.Group;
			if( group is UnanchoredPanelGroup )
			{
				Debug.LogError( "Can not anchor to an unanchored panel!" );
				return;
			}

			Vector2 size;
			Panel panel = source as Panel;
			if( panel != null )
				size = panel.IsDocked ? panel.FloatingSize : panel.Size;
			else
			{
				( (PanelGroup) source ).Internal.UpdateLayout();
				size = source.Size;
			}

			// Fill the whole anchored area in order not to break other elements' sizes on layout update
			if( anchorDirection == Direction.Left || anchorDirection == Direction.Right )
			{
				if( anchor.Size.y > 0f )
					size.y = anchor.Size.y;
			}
			else
			{
				if( anchor.Size.x > 0f )
					size.x = anchor.Size.x;
			}

			if( panel != null )
				panel.RectTransform.sizeDelta = size;
			else
				( (PanelGroup) source ).Internal.UpdateBounds( source.Position, size );

			bool addElementAfter = anchorDirection == Direction.Right || anchorDirection == Direction.Top;
			if( group.IsInSameDirection( anchorDirection ) )
			{
				if( addElementAfter )
					group.AddElementAfter( anchor, source );
				else
					group.AddElementBefore( anchor, source );
			}
			else
			{
				IPanelGroupElement element1, element2;
				if( addElementAfter )
				{
					element1 = anchor;
					element2 = source;
				}
				else
				{
					element1 = source;
					element2 = anchor;
				}

				PanelGroup newGroup = new PanelGroup( anchor.Canvas, anchorDirection );
				newGroup.AddElement( element1 );
				newGroup.AddElement( element2 );

				group.Internal.ReplaceElement( anchor, newGroup );
			}

			if( panel != null )
			{
				if( draggedPanel == panel )
					draggedPanel = null;

				panel.RectTransform.SetAsFirstSibling();

				if( panel.Internal.ContentScrollRect != null )
					panel.Internal.ContentScrollRect.OnDrag( nullPointerEventData );
			}
		}

		public void DetachPanel( Panel panel )
		{
			if( draggedPanel == panel )
				draggedPanel = null;

			if( panel.IsDocked )
			{
				panel.Canvas.UnanchoredPanelGroup.AddElement( panel );
				panel.RectTransform.SetAsLastSibling();
				panel.RectTransform.sizeDelta = panel.FloatingSize;

				if( panel.Internal.ContentScrollRect != null )
					panel.Internal.ContentScrollRect.OnDrag( nullPointerEventData );
			}
		}

		public Panel DetachPanelTab( Panel panel, int tabIndex )
		{
			if( tabIndex >= 0 && tabIndex < panel.NumberOfTabs )
			{
				if( panel.NumberOfTabs == 1 )
				{
					DetachPanel( panel );
					return panel;
				}
				else
				{
					Panel newPanel = PanelUtils.Internal.CreatePanel( null, panel.Canvas );
					newPanel.AddTab( panel[tabIndex].Content );
					newPanel.FloatingSize = panel.FloatingSize;

					if( newPanel.Internal.ContentScrollRect != null )
						newPanel.Internal.ContentScrollRect.OnDrag( nullPointerEventData );

					return newPanel;
				}
			}

			return null;
		}

		private void BringClickedPanelToForward( Vector2 screenPoint )
		{
			for( int i = 0; i < canvases.Count; i++ )
			{
				if( !canvases[i].gameObject.activeInHierarchy )
					continue;

				Camera worldCamera = canvases[i].Internal.worldCamera;
				if( RectTransformUtility.RectangleContainsScreenPoint( canvases[i].RectTransform, screenPoint, worldCamera ) )
				{
					RectTransform panelAtTop = null;
					for( int j = 0; j < panels.Count; j++ )
					{
						if( panels[j].IsDocked || panels[j].Internal.IsDummy )
							continue;

						if( panels[j].Canvas == canvases[i] && RectTransformUtility.RectangleContainsScreenPoint( panels[j].Internal.HighlightTransform, screenPoint, worldCamera ) )
						{
							if( panelAtTop == null || panels[j].RectTransform.GetSiblingIndex() > panelAtTop.GetSiblingIndex() )
								panelAtTop = panels[j].RectTransform;
						}
					}

					if( panelAtTop != null )
					{
						panelAtTop.SetAsLastSibling();
						return;
					}
				}
			}
		}

		public void StopCanvasOperations( DynamicPanelsCanvas canvas )
		{
			CancelDraggingPanel();

			for( int i = 0; i < panels.Count; i++ )
			{
				if( panels[i].Canvas == canvas )
					panels[i].Internal.Stop();
			}
		}

		public void CancelDraggingPanel()
		{
			if( draggedPanel != null )
			{
				if( draggedPanel.RectTransform.parent != draggedPanel.Canvas.RectTransform )
				{
					draggedPanel.RectTransform.SetParent( draggedPanel.Canvas.RectTransform, false );
					draggedPanel.RectTransform.SetAsLastSibling();
				}

				AnchorZonesSetActive( false );

				UnanchoredPanelGroup unanchoredGroup = draggedPanel.Group as UnanchoredPanelGroup;
				if( unanchoredGroup != null )
					unanchoredGroup.RestrictPanelToBounds( draggedPanel );

				draggedPanel.Internal.Stop();
				draggedPanel = null;
			}

			hoveredAnchorZone = null;

			if( previewPanel.gameObject.activeSelf )
				previewPanel.gameObject.SetActive( false );
		}

		public void OnPointerEnteredCanvas( DynamicPanelsCanvas canvas, PointerEventData pointer )
		{
			if( draggedPanel != null && pointer.pointerDrag != null )
			{
				PanelHeader header = pointer.pointerDrag.GetComponent<PanelHeader>();
				if( header != null )
				{
					if( header.Panel == draggedPanel && header.Panel.RectTransform.parent != canvas.RectTransform )
					{
						previewPanelCanvas = canvas;

						header.Panel.RectTransform.SetParent( canvas.RectTransform, false );
						header.Panel.RectTransform.SetAsLastSibling();
					}
				}
				else
				{
					PanelTab tab = pointer.pointerDrag.GetComponent<PanelTab>();
					if( tab != null )
					{
						if( tab.Panel == draggedPanel && previewPanel.parent != canvas.RectTransform )
						{
							previewPanelCanvas = canvas;

							if( hoveredAnchorZone && hoveredAnchorZone.Panel.Canvas != canvas )
								hoveredAnchorZone.OnPointerExit( pointer );

							previewPanel.SetParent( canvas.RectTransform, false );
							previewPanel.SetAsLastSibling();
						}
					}
				}
			}
		}

		#region Panel Header Drag Callbacks
		public bool OnBeginPanelTranslate( Panel panel )
		{
			if( panel.IsDocked )
				return false;

			if( draggedPanel != null )
				CancelDraggingPanel();

			draggedPanel = panel;
			previewPanelCanvas = draggedPanel.Canvas;

			for( int i = 0; i < canvases.Count; i++ )
				canvases[i].Internal.ReceiveRaycasts( true );

			return true;
		}

		public void OnPanelTranslate( PanelHeader panelHeader, PointerEventData draggingPointer )
		{
			if( draggedPanel == panelHeader.Panel )
			{
				Vector2 touchPos;
				RectTransformUtility.ScreenPointToLocalPointInRectangle( draggedPanel.RectTransform, draggingPointer.position, previewPanelCanvas.Internal.worldCamera, out touchPos );

				draggedPanel.RectTransform.anchoredPosition += touchPos - panelHeader.InitialTouchPos;
			}
		}

		public void OnEndPanelTranslate( Panel panel )
		{
			if( draggedPanel == panel )
			{
				if( draggedPanel.RectTransform.parent != draggedPanel.Canvas.RectTransform )
					draggedPanel.RectTransform.parent.GetComponent<DynamicPanelsCanvas>().UnanchoredPanelGroup.AddElement( draggedPanel );

				for( int i = 0; i < canvases.Count; i++ )
					canvases[i].Internal.ReceiveRaycasts( false );

				draggedPanel = null;
				( (UnanchoredPanelGroup) panel.Group ).RestrictPanelToBounds( panel );
			}
		}
		#endregion

		#region Panel Tab Drag Callbacks
		public bool OnBeginPanelTabTranslate( PanelTab panelTab, PointerEventData draggingPointer )
		{
			if( draggedPanel != null )
				CancelDraggingPanel();

			if( panelTab.Panel.NumberOfTabs == 1 && panelTab.Panel.Canvas.PreventDetachingLastDockedPanel && panelTab.Panel.Canvas.Internal.IsLastDockedPanel( panelTab.Panel ) )
				return false;

			draggedPanel = panelTab.Panel;

			if( !draggedPanel.IsDocked )
				draggedPanel.FloatingSize = draggedPanel.Size;

			AnchorZonesSetActive( true );

			if( draggedPanel.NumberOfTabs <= 1 )
				draggedPanel.Internal.PanelAnchorZone.SetActive( false );

			if( RectTransformUtility.RectangleContainsScreenPoint( draggedPanel.Internal.HeaderAnchorZone.RectTransform, draggingPointer.position, draggedPanel.Canvas.Internal.worldCamera ) )
			{
				draggedPanel.Internal.HeaderAnchorZone.OnPointerEnter( draggingPointer );
				draggingPointer.pointerEnter = draggedPanel.Internal.HeaderAnchorZone.gameObject;
			}

			previewPanelCanvas = draggedPanel.Canvas;
			if( previewPanel.parent != previewPanelCanvas.RectTransform )
				previewPanel.SetParent( previewPanelCanvas.RectTransform, false );

			previewPanel.gameObject.SetActive( true );
			previewPanel.SetAsLastSibling();

			return true;
		}

		public void OnPanelTabTranslate( PanelTab panelTab, PointerEventData draggingPointer )
		{
			if( draggedPanel == panelTab.Panel )
			{
				Rect previewRect;
				if( hoveredAnchorZone != null && hoveredAnchorZone.GetAnchoredPreviewRectangleAt( draggingPointer, out previewRect ) )
				{
					previewPanel.anchoredPosition = previewRect.position;
					previewPanel.sizeDelta = previewRect.size;
				}
				else
				{
					Vector2 position;
					RectTransformUtility.ScreenPointToLocalPointInRectangle( previewPanelCanvas.RectTransform, draggingPointer.position, previewPanelCanvas.Internal.worldCamera, out position );
					previewPanel.anchoredPosition = position;
					previewPanel.sizeDelta = panelTab.Panel.FloatingSize;
				}
			}
		}

		public void OnEndPanelTabTranslate( PanelTab panelTab, PointerEventData draggingPointer )
		{
			if( draggedPanel == panelTab.Panel )
			{
				AnchorZoneBase targetAnchor = hoveredAnchorZone;
				if( hoveredAnchorZone != null )
					hoveredAnchorZone.OnPointerExit( draggingPointer );

				AnchorZonesSetActive( false );

				if( targetAnchor == null || !targetAnchor.Execute( panelTab, draggingPointer ) )
				{
					Panel detachedPanel = DetachPanelTab( draggedPanel, draggedPanel.GetTabIndex( panelTab.Content ) );
					if( detachedPanel.Canvas != previewPanelCanvas )
					{
						previewPanelCanvas.UnanchoredPanelGroup.AddElement( detachedPanel );
						detachedPanel.RectTransform.SetAsLastSibling();
					}

					detachedPanel.MoveTo( draggingPointer.position );
				}

				draggedPanel = null;
				hoveredAnchorZone = null;
				previewPanel.gameObject.SetActive( false );
			}
		}
		#endregion

		#region Preview Panel Handlers
		public bool AnchorPreviewPanelTo( AnchorZoneBase anchorZone )
		{
			if( hoveredAnchorZone != null || draggedPanel == null )
				return false;

			hoveredAnchorZone = anchorZone;
			return true;
		}

		public void StopAnchorPreviewPanelTo( AnchorZoneBase anchorZone )
		{
			if( hoveredAnchorZone == anchorZone )
				hoveredAnchorZone = null;
		}
		#endregion

		private void AnchorZonesSetActive( bool value )
		{
			for( int i = 0; i < panels.Count; i++ )
				panels[i].Internal.AnchorZonesSetActive( value );

			for( int i = 0; i < canvases.Count; i++ )
			{
				canvases[i].Internal.AnchorZonesSetActive( value );
				canvases[i].Internal.ReceiveRaycasts( value );
			}
		}

		private void InitializePreviewPanel()
		{
			RectTransform previewPanel = Instantiate( Resources.Load<RectTransform>( "DynamicPanelPreview" ) );
			previewPanel.gameObject.name = "DraggedPanelPreview";

			previewPanel.anchorMin = new Vector2( 0.5f, 0.5f );
			previewPanel.anchorMax = new Vector2( 0.5f, 0.5f );
			previewPanel.pivot = new Vector2( 0.5f, 0.5f );

			previewPanel.gameObject.SetActive( false );

			this.previewPanel = previewPanel;
		}
	}
}