using UnityEngine;
using UnityEngine.EventSystems;

namespace DynamicPanels
{
	public class PanelHeaderAnchorZone : AnchorZoneBase
	{
		public override bool Execute( PanelTab panelTab, PointerEventData eventData )
		{
			Vector2 tabPreviewRect;
			int tabIndex = m_panel.Internal.GetTabIndexAt( eventData, out tabPreviewRect );

			m_panel.AddTab( panelTab.Content, tabIndex );
			return true;
		}

		public override bool GetAnchoredPreviewRectangleAt( PointerEventData eventData, out Rect rect )
		{
			Vector2 tabPreviewRect;
			m_panel.Internal.GetTabIndexAt( eventData, out tabPreviewRect );

			rect = new Rect( tabPreviewRect.x, m_panel.RectTransform.sizeDelta.y - m_panel.Internal.HeaderHeight, tabPreviewRect.y, m_panel.Internal.HeaderHeight );
			rect.position += m_panel.RectTransform.anchoredPosition + ( rect.size - m_panel.Canvas.Size ) * 0.5f;

			return true;
		}
	}
}