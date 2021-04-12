using UnityEngine;

namespace DynamicPanels
{
	public static class PanelUtils
	{
		internal static class Internal
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
						if( !canvas )
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
			if( !content )
			{
				Debug.LogError( "Content is null!" );
				return null;
			}

			return Internal.CreatePanel( content, canvas );
		}

		public static PanelTab GetAssociatedTab( RectTransform content )
		{
			if( !content )
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

		// Credit: https://github.com/Unity-Technologies/UnityCsReference/blob/4fc5eb0fb2c7f5fb09f990fc99f162c8d06d9570/Editor/Mono/Inspector/RectTransformEditor.cs#L1259
		public static void ChangePivotWithoutAffectingPosition( this RectTransform rectTransform, Vector2 pivot )
		{
			if( rectTransform.pivot == pivot )
				return;

			Vector3 cornerBefore;
			Vector3[] s_Corners = new Vector3[4];
			rectTransform.GetWorldCorners( s_Corners );
			if( rectTransform.parent )
				cornerBefore = rectTransform.parent.InverseTransformPoint( s_Corners[0] );
			else
				cornerBefore = s_Corners[0];

			rectTransform.pivot = pivot;

			Vector3 cornerAfter;
			rectTransform.GetWorldCorners( s_Corners );
			if( rectTransform.parent )
				cornerAfter = rectTransform.parent.InverseTransformPoint( s_Corners[0] );
			else
				cornerAfter = s_Corners[0];

			Vector3 cornerOffset = cornerAfter - cornerBefore;
			rectTransform.anchoredPosition -= (Vector2) cornerOffset;

			Vector3 pos = rectTransform.transform.position;
			pos.z -= cornerOffset.z;
			rectTransform.transform.position = pos;
		}
	}
}