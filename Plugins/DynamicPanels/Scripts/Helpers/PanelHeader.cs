using UnityEngine;
using UnityEngine.EventSystems;

namespace DynamicPanels
{
	[DisallowMultipleComponent]
	public class PanelHeader : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
#pragma warning disable 0649
		[SerializeField]
		private Panel m_panel;
		public Panel Panel { get { return m_panel; } }
#pragma warning restore 0649

		private int pointerId = PanelManager.NON_EXISTING_TOUCH;

		private Vector2 m_initialTouchPos;
		internal Vector2 InitialTouchPos { get { return m_initialTouchPos; } }

		private void OnEnable()
		{
			pointerId = PanelManager.NON_EXISTING_TOUCH;
		}

		public void OnBeginDrag( PointerEventData eventData )
		{
			if( !PanelManager.Instance.OnBeginPanelTranslate( m_panel ) )
			{
				eventData.pointerDrag = null;
				return;
			}

			pointerId = eventData.pointerId;
			RectTransformUtility.ScreenPointToLocalPointInRectangle( m_panel.RectTransform, eventData.position, m_panel.Canvas.Internal.worldCamera, out m_initialTouchPos );
		}

		public void OnDrag( PointerEventData eventData )
		{
			if( eventData.pointerId != pointerId )
			{
				eventData.pointerDrag = null;
				return;
			}

			PanelManager.Instance.OnPanelTranslate( this, eventData );
		}

		public void OnEndDrag( PointerEventData eventData )
		{
			if( eventData.pointerId != pointerId )
				return;

			pointerId = PanelManager.NON_EXISTING_TOUCH;
			PanelManager.Instance.OnEndPanelTranslate( m_panel );
		}

		public void Stop()
		{
			pointerId = PanelManager.NON_EXISTING_TOUCH;
		}
	}
}