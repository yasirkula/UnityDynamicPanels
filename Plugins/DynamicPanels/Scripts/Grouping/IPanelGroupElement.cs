using UnityEngine;

namespace DynamicPanels
{
	public interface IPanelGroupElement
	{
		DynamicPanelsCanvas Canvas { get; }
		PanelGroup Group { get; }

		Vector2 Position { get; }
		Vector2 Size { get; }
		Vector2 MinSize { get; }

		void ResizeTo( Vector2 newSize );

		void DockToRoot( Direction direction );
		void DockToPanel( IPanelGroupElement anchor, Direction direction );

		IPanelGroupElement GetSurroundingElement( Direction direction );
	}
}