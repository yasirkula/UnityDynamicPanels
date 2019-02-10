using UnityEngine;

namespace DynamicPanels
{
	public static class PanelUtils
	{
		public static class Internal
		{
			public static Panel CreatePanel( RectTransform content, DynamicPanelsCanvas canvas )
			{
				bool canvasWasNull = canvas == null;
				if( canvasWasNull )
				{
					if( content != null )
						canvas = content.GetComponentInParent<DynamicPanelsCanvas>();

					if( canvas == null )
					{
						canvas = Object.FindObjectOfType<DynamicPanelsCanvas>();
						if( canvas == null || canvas.Equals( null ) )
						{
							Debug.LogError( "Panels require a DynamicPanelsCanvas!" );
							return null;
						}
					}
				}

				Panel result = null;
				if( content != null )
				{
					PanelTab currentTab = GetAssociatedTab( content );
					if( currentTab != null )
						result = currentTab.Panel;
				}

				if( result == null )
				{
					result = (Panel) Object.Instantiate( Resources.Load<Panel>( "DynamicPanel" ), canvas.RectTransform, false );
					result.gameObject.name = "DynamicPanel";
					result.RectTransform.SetAsLastSibling();

					if( content != null )
					{
						Rect contentRect = content.rect;

						result.RectTransform.anchoredPosition = (Vector2) canvas.RectTransform.InverseTransformPoint( content.TransformPoint( contentRect.position ) ) + canvas.Size * 0.5f;
						result.FloatingSize = contentRect.size;
					}
				}
				else if( result.Canvas != canvas && !canvasWasNull )
					canvas.UnanchoredPanelGroup.AddElement( result );

				if( content != null )
					result.AddTab( content );

				return result;
			}
		}

		public static Panel CreatePanelFor( RectTransform content, DynamicPanelsCanvas canvas )
		{
			if( content == null || content.Equals( null ) )
			{
				Debug.LogError( "Content is null!" );
				return null;
			}

			return Internal.CreatePanel( content, canvas );
		}

		public static PanelTab GetAssociatedTab( RectTransform content )
		{
			if( content == null || content.Equals( null ) )
			{
				Debug.LogError( "Content is null!" );
				return null;
			}

			if( content.parent == null || content.parent.parent == null )
				return null;

			Panel panel = content.parent.parent.GetComponent<Panel>();
			if( panel == null )
				return null;

			return panel.GetTab( content );
		}

		public static Direction Opposite( this Direction direction )
		{
			return (Direction) ( ( (int) direction + 2 ) % 4 );
		}

		public static bool IsNull( this IPanelGroupElement element )
		{
			return element == null || element.Equals( null );
		}
	}
}