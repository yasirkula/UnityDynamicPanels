using UnityEngine;

namespace DynamicPanels
{
	public static class PanelUtils
	{
		public static class Internal
		{
			public static Panel CreatePanel( RectTransform content, DynamicPanelsCanvas canvas )
			{
				if( canvas == null )
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

				Panel result = content != null ? content.GetComponentInParent<Panel>() : null;
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