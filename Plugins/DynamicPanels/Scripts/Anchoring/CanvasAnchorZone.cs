using UnityEngine;
using UnityEngine.EventSystems;

namespace DynamicPanels
{
	public class CanvasAnchorZone : AnchorZoneBase
	{
		private Direction direction;

		public void SetDirection( Direction direction )
		{
			this.direction = direction;
		}

		public override bool Execute( PanelTab panelTab, PointerEventData eventData )
		{
			Panel detachedPanel = PanelManager.Instance.DetachPanelTab( panelTab.Panel, panelTab.Panel.GetTabIndex( panelTab ) );
			PanelManager.Instance.AnchorPanel( detachedPanel, m_panel.Canvas, direction );

			return true;
		}

		public override bool GetAnchoredPreviewRectangleAt( PointerEventData eventData, out Rect rect )
		{
			Vector2 size = m_panel.Canvas.Size;
			if( direction == Direction.Left )
				rect = new Rect( 0f, 0f, size.x * 0.2f, size.y );
			else if( direction == Direction.Top )
				rect = new Rect( 0f, size.y * 0.8f, size.x, size.y * 0.2f );
			else if( direction == Direction.Right )
				rect = new Rect( size.x * 0.8f, 0f, size.x * 0.2f, size.y );
			else
				rect = new Rect( 0f, 0f, size.x, size.y * 0.2f );

			rect.position += ( rect.size - size ) * 0.5f;
			return true;
		}
	}
}