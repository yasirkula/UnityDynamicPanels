using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DynamicPanels
{
	[DisallowMultipleComponent]
	public abstract class AnchorZoneBase : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerExitHandler
	{
		protected Panel m_panel;
		public Panel Panel { get { return m_panel; } }

		public RectTransform RectTransform { get; private set; }

		private Graphic raycastZone;

		private int hoveredPointerId = PanelManager.NON_EXISTING_TOUCH;

		public DynamicPanelsCanvas Canvas { get { return m_panel.Canvas; } }

		protected void Awake()
		{
			RectTransform = (RectTransform) transform;
			raycastZone = gameObject.AddComponent<NonDrawingGraphic>();
		}

		protected void OnEnable()
		{
			hoveredPointerId = PanelManager.NON_EXISTING_TOUCH;
		}

		public abstract bool Execute( PanelTab panelTab, PointerEventData eventData );
		public abstract bool GetAnchoredPreviewRectangleAt( PointerEventData eventData, out Rect rect );

		public void Initialize( Panel panel )
		{
			m_panel = panel;
		}

		public void SetActive( bool value )
		{
			hoveredPointerId = PanelManager.NON_EXISTING_TOUCH;
			raycastZone.raycastTarget = value;
		}

		public void OnPointerEnter( PointerEventData eventData )
		{
			if( PanelManager.Instance.AnchorPreviewPanelTo( this ) )
				hoveredPointerId = eventData.pointerId;
		}

		// Saves the system from a complete shutdown in a rare case
		public void OnPointerDown( PointerEventData eventData )
		{
			PanelManager.Instance.CancelDraggingPanel();
		}

		public void OnPointerExit( PointerEventData eventData )
		{
			if( eventData.pointerId == hoveredPointerId )
			{
				hoveredPointerId = PanelManager.NON_EXISTING_TOUCH;
				PanelManager.Instance.StopAnchorPreviewPanelTo( this );
			}
		}
	}
}