#if UNITY_EDITOR || ( !UNITY_ANDROID && !UNITY_IOS )
#define ENABLE_CURSOR_MANAGEMENT
#endif

using UnityEngine;
using UnityEngine.EventSystems;

namespace DynamicPanels
{
	[DisallowMultipleComponent]
	public class PanelResizeHelper : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
#if ENABLE_CURSOR_MANAGEMENT
		, IPointerEnterHandler, IPointerExitHandler
#endif
	{
		private Panel m_panel;
		public Panel Panel { get { return m_panel; } }

		public RectTransform RectTransform { get; private set; }

		private Direction m_direction;
		private Direction secondDirection;

		public Direction Direction { get { return m_direction; } }

		private PanelResizeHelper helperBefore, helperAfter;

		private int pointerId = PanelManager.NON_EXISTING_TOUCH;

		private void Awake()
		{
			RectTransform = (RectTransform) transform;
		}

		private void OnEnable()
		{
			pointerId = PanelManager.NON_EXISTING_TOUCH;
		}

		public void Initialize( Panel panel, Direction direction, PanelResizeHelper helperBefore, PanelResizeHelper helperAfter )
		{
			m_panel = panel;

			this.m_direction = direction;
			this.helperBefore = helperBefore;
			this.helperAfter = helperAfter;
		}

#if ENABLE_CURSOR_MANAGEMENT
		public void OnPointerEnter( PointerEventData eventData )
		{
			PanelCursorHandler.OnPointerEnter( this, eventData );
		}

		public void OnPointerExit( PointerEventData eventData )
		{
			PanelCursorHandler.OnPointerExit( this );
		}
#endif

		public void OnBeginDrag( PointerEventData eventData )
		{
			// Cancel drag event if panel is already being resized by another pointer
			// or panel is anchored to a fixed anchor in that direction
			if( !m_panel.CanResizeInDirection( m_direction ) )
			{
				eventData.pointerDrag = null;
				return;
			}

			pointerId = eventData.pointerId;
			secondDirection = GetSecondDirection( eventData.pressPosition );

#if ENABLE_CURSOR_MANAGEMENT
			PanelCursorHandler.OnBeginResize( m_direction, secondDirection );
#endif
		}

		public void OnDrag( PointerEventData eventData )
		{
			if( eventData.pointerId != pointerId )
				return;

			m_panel.Internal.OnResize( m_direction, eventData.position );

			if( secondDirection != Direction.None )
				m_panel.Internal.OnResize( secondDirection, eventData.position );
		}

		public void OnEndDrag( PointerEventData eventData )
		{
			if( eventData.pointerId != pointerId )
				return;

			if( !m_panel.IsDocked )
				( (UnanchoredPanelGroup) m_panel.Group ).RestrictPanelToBounds( m_panel );

			pointerId = PanelManager.NON_EXISTING_TOUCH;

#if ENABLE_CURSOR_MANAGEMENT
			PanelCursorHandler.OnEndResize();
#endif
		}

		public Direction GetSecondDirection( Vector2 pointerPosition )
		{
			if( m_panel.IsDocked )
				return Direction.None;

			Direction result;
			if( RectTransformUtility.RectangleContainsScreenPoint( helperBefore.RectTransform, pointerPosition, m_panel.Canvas.Internal.worldCamera ) )
				result = helperBefore.m_direction;
			else if( RectTransformUtility.RectangleContainsScreenPoint( helperAfter.RectTransform, pointerPosition, m_panel.Canvas.Internal.worldCamera ) )
				result = helperAfter.m_direction;
			else
				result = Direction.None;

			if( !m_panel.CanResizeInDirection( result ) )
				result = Direction.None;

			return result;
		}

		public void Stop()
		{
			if( pointerId != PanelManager.NON_EXISTING_TOUCH )
			{
				if( !m_panel.IsDocked )
					( (UnanchoredPanelGroup) m_panel.Group ).RestrictPanelToBounds( m_panel );

				pointerId = PanelManager.NON_EXISTING_TOUCH;

#if ENABLE_CURSOR_MANAGEMENT
				PanelCursorHandler.OnEndResize();
#endif
			}
		}
	}
}