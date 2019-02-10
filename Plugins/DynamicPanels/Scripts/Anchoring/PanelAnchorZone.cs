using UnityEngine;
using UnityEngine.EventSystems;

namespace DynamicPanels
{
	public class PanelAnchorZone : AnchorZoneBase
	{
		public override bool Execute( PanelTab panelTab, PointerEventData eventData )
		{
			Direction anchorDirection = GetAnchorDirection( eventData );
			if( anchorDirection == Direction.None )
				return false;

			Panel detachedPanel = PanelManager.Instance.DetachPanelTab( panelTab.Panel, panelTab.Panel.GetTabIndex( panelTab ) );
			PanelManager.Instance.AnchorPanel( detachedPanel, m_panel, anchorDirection );

			return true;
		}

		public override bool GetAnchoredPreviewRectangleAt( PointerEventData eventData, out Rect rect )
		{
			Direction anchorDirection = GetAnchorDirection( eventData );
			if( anchorDirection == Direction.None )
			{
				rect = new Rect();
				return false;
			}

			Vector2 size = m_panel.RectTransform.sizeDelta;
			size.y -= m_panel.Internal.HeaderHeight;

			float anchorWidth = Mathf.Min( m_panel.Canvas.PanelAnchorZoneLength, size.x * m_panel.Canvas.PanelAnchorZoneLengthRatio );
			float anchorHeight = Mathf.Min( m_panel.Canvas.PanelAnchorZoneLength, size.y * m_panel.Canvas.PanelAnchorZoneLengthRatio );

			if( anchorDirection == Direction.Left )
				rect = new Rect( 0f, 0f, anchorWidth, size.y );
			else if( anchorDirection == Direction.Top )
				rect = new Rect( 0f, size.y - anchorHeight, size.x, anchorHeight );
			else if( anchorDirection == Direction.Right )
				rect = new Rect( size.x - anchorWidth, 0f, anchorWidth, size.y );
			else
				rect = new Rect( 0f, 0f, size.x, anchorHeight );

			rect.position += m_panel.RectTransform.anchoredPosition + ( rect.size - m_panel.Canvas.Size ) * 0.5f;
			return true;
		}

		private Direction GetAnchorDirection( PointerEventData eventData )
		{
			Vector2 pointerPos;
			RectTransformUtility.ScreenPointToLocalPointInRectangle( m_panel.RectTransform, eventData.position, m_panel.Canvas.Internal.worldCamera, out pointerPos );

			Vector2 size = m_panel.RectTransform.sizeDelta;
			size.y -= m_panel.Internal.HeaderHeight;

			float anchorWidth = Mathf.Min( m_panel.Canvas.PanelAnchorZoneLength, size.x * m_panel.Canvas.PanelAnchorZoneLengthRatio );
			float anchorHeight = Mathf.Min( m_panel.Canvas.PanelAnchorZoneLength, size.y * m_panel.Canvas.PanelAnchorZoneLengthRatio );

			if( pointerPos.y < anchorHeight )
				return Direction.Bottom;
			if( pointerPos.y > size.y - anchorHeight )
				return Direction.Top;
			if( pointerPos.x < anchorWidth )
				return Direction.Left;
			if( pointerPos.x > size.x - anchorWidth )
				return Direction.Right;

			return Direction.None;
		}
	}
}