using UnityEngine;
using UnityEngine.EventSystems;

namespace DynamicPanels
{
	[DisallowMultipleComponent]
	public class PanelResizeHelper : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		private Panel m_panel;
		public Panel Panel { get { return m_panel; } }

		public RectTransform RectTransform { get; private set; }

		private Direction direction;
		private Direction secondDirection;

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

			this.direction = direction;
			this.helperBefore = helperBefore;
			this.helperAfter = helperAfter;
		}

		public void OnBeginDrag( PointerEventData eventData )
		{
			// Cancel drag event if panel is already being resized by another pointer
			// or panel is anchored to a fixed anchor in that direction
			if( !m_panel.Internal.CanResizeInDirection( direction ) )
			{
				eventData.pointerDrag = null;
				return;
			}

			pointerId = eventData.pointerId;

			if( m_panel.IsDocked )
				secondDirection = Direction.None;
			else
			{
				if( RectTransformUtility.RectangleContainsScreenPoint( helperBefore.RectTransform, eventData.pressPosition, m_panel.Canvas.Internal.worldCamera ) )
					secondDirection = helperBefore.direction;
				else if( RectTransformUtility.RectangleContainsScreenPoint( helperAfter.RectTransform, eventData.pressPosition, m_panel.Canvas.Internal.worldCamera ) )
					secondDirection = helperAfter.direction;
				else
					secondDirection = Direction.None;

				if( !m_panel.Internal.CanResizeInDirection( secondDirection ) )
					secondDirection = Direction.None;
			}
		}

		public void OnDrag( PointerEventData eventData )
		{
			if( eventData.pointerId != pointerId )
				return;

			m_panel.Internal.OnResize( direction, eventData.position );

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
		}

		public void Stop()
		{
			if( pointerId != PanelManager.NON_EXISTING_TOUCH )
			{
				if( !m_panel.IsDocked )
					( (UnanchoredPanelGroup) m_panel.Group ).RestrictPanelToBounds( m_panel );

				pointerId = PanelManager.NON_EXISTING_TOUCH;
			}
		}
	}
}