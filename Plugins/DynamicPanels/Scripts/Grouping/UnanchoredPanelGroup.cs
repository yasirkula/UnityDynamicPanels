using UnityEngine;

namespace DynamicPanels
{
	public class UnanchoredPanelGroup : PanelGroup
	{
		public UnanchoredPanelGroup( DynamicPanelsCanvas canvas ) : base( canvas, Direction.None )
		{
		}

		protected override void UpdateBounds( Vector2 position, Vector2 size )
		{
			for( int i = 0; i < elements.Count; i++ )
			{
				if( elements[i] is Panel )
					RestrictPanelToBounds( (Panel) elements[i], size );
			}
		}

		protected override void UpdateLayout()
		{
			bool wasDirty = isDirty;

			base.UpdateLayout();

			if( wasDirty )
			{
				for( int i = elements.Count - 1; i >= 0; i-- )
				{
					PanelGroup subGroup = elements[i] as PanelGroup;
					if( subGroup != null )
					{
						elements.RemoveAt( i );

						for( int j = 0; j < subGroup.Count; j++, i++ )
						{
							elements.Insert( i, subGroup[j] );
							SetGroupFor( elements[i], this );
						}
					}
				}
			}
		}

		protected override void EnsureMinimumSizeOf( IPanelGroupElement element )
		{
			Panel panel = element as Panel;
			if( !panel )
				return;

			Vector2 position = panel.Position;

			Vector2 size = panel.Size;
			Vector2 minSize = panel.MinSize;

			bool hasChanged = false;

			float flexibleWidth = size.x - minSize.x;
			if( flexibleWidth < -MIN_SIZE_TOLERANCE )
			{
				size.x -= flexibleWidth;
				position.x += flexibleWidth * 0.5f;

				hasChanged = true;
			}

			float flexibleHeight = size.y - minSize.y;
			if( flexibleHeight < -MIN_SIZE_TOLERANCE )
			{
				size.y -= flexibleHeight;
				position.y += flexibleHeight * 0.5f;

				hasChanged = true;
			}

			if( hasChanged )
			{
				panel.Internal.UpdateBounds( position, size );
				RestrictPanelToBounds( panel );
			}
		}

		public void RestrictPanelToBounds( Panel panel )
		{
			RestrictPanelToBounds( panel, Canvas.Size );
		}

		protected void RestrictPanelToBounds( Panel panel, Vector2 canvasSize )
		{
			Vector2 panelPosition = panel.RectTransform.anchoredPosition;
			Vector2 panelSize = panel.RectTransform.sizeDelta;

			if( panelPosition.y + panelSize.y < 50f )
				panelPosition.y = 50f - panelSize.y;
			else if( panelPosition.y + panelSize.y > canvasSize.y )
				panelPosition.y = canvasSize.y - panelSize.y;

			if( panelPosition.x < 0f )
				panelPosition.x = 0f;
			else if( canvasSize.x - panelPosition.x < 125f )
				panelPosition.x = canvasSize.x - 125f;

			panel.RectTransform.anchoredPosition = panelPosition;
		}

		public override string ToString()
		{
			return "Unanchored Panel Group";
		}
	}
}