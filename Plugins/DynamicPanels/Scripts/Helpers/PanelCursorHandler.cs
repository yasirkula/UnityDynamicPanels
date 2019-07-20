using UnityEngine;
using UnityEngine.EventSystems;

namespace DynamicPanels
{
	public class PanelCursorHandler : MonoBehaviour
	{
		private static PanelCursorHandler instance = null;

		private PanelResizeHelper activeResizeHelper;
		private PointerEventData activeEventData;

		private bool isResizing;
		private Vector2 prevPointerPos;

#pragma warning disable 0649
		[SerializeField]
		private Texture2D horizontalCursor;
		[SerializeField]
		private Texture2D verticalCursor;
		[SerializeField]
		private Texture2D diagonalCursorTopLeft;
		[SerializeField]
		private Texture2D diagonalCursorTopRight;
#pragma warning restore 0649

		private void Awake()
		{
			instance = this;
		}

		public static void OnPointerEnter( PanelResizeHelper resizeHelper, PointerEventData eventData )
		{
			if( instance == null )
				return;

			instance.activeResizeHelper = resizeHelper;
			instance.activeEventData = eventData;
		}

		public static void OnPointerExit( PanelResizeHelper resizeHelper )
		{
			if( instance == null )
				return;

			if( instance.activeResizeHelper == resizeHelper )
			{
				instance.activeResizeHelper = null;
				instance.activeEventData = null;

				if( !instance.isResizing )
					SetDefaultCursor();
			}
		}

		public static void OnBeginResize( Direction primary, Direction secondary )
		{
			if( instance == null )
				return;

			instance.isResizing = true;
			instance.UpdateCursor( primary, secondary );
		}

		public static void OnEndResize()
		{
			if( instance == null )
				return;

			instance.isResizing = false;

			if( instance.activeResizeHelper == null )
				SetDefaultCursor();
			else
				instance.prevPointerPos = new Vector2( -1f, -1f );
		}

		private void Update()
		{
			if( isResizing )
				return;

			if( activeResizeHelper != null )
			{
				Vector2 pointerPos = activeEventData.position;
				if( pointerPos != prevPointerPos )
				{
					if( activeEventData.dragging )
						SetDefaultCursor();
					else
					{
						Direction direction = activeResizeHelper.Direction;
						Direction secondDirection = activeResizeHelper.GetSecondDirection( activeEventData.position );
						if( activeResizeHelper.Panel.CanResizeInDirection( direction ) )
							UpdateCursor( direction, secondDirection );
						else if( secondDirection != Direction.None )
							UpdateCursor( secondDirection, Direction.None );
						else
							SetDefaultCursor();
					}

					prevPointerPos = pointerPos;
				}
			}
		}

		private static void SetDefaultCursor()
		{
			Cursor.SetCursor( null, Vector2.zero, CursorMode.Auto );
		}

		private void UpdateCursor( Direction primary, Direction secondary )
		{
			Texture2D cursorTex;
			if( primary == Direction.Left )
			{
				if( secondary == Direction.Top )
					cursorTex = diagonalCursorTopLeft;
				else if( secondary == Direction.Bottom )
					cursorTex = diagonalCursorTopRight;
				else
					cursorTex = horizontalCursor;
			}
			else if( primary == Direction.Right )
			{
				if( secondary == Direction.Top )
					cursorTex = diagonalCursorTopRight;
				else if( secondary == Direction.Bottom )
					cursorTex = diagonalCursorTopLeft;
				else
					cursorTex = horizontalCursor;
			}
			else if( primary == Direction.Top )
			{
				if( secondary == Direction.Left )
					cursorTex = diagonalCursorTopLeft;
				else if( secondary == Direction.Right )
					cursorTex = diagonalCursorTopRight;
				else
					cursorTex = verticalCursor;
			}
			else
			{
				if( secondary == Direction.Left )
					cursorTex = diagonalCursorTopRight;
				else if( secondary == Direction.Right )
					cursorTex = diagonalCursorTopLeft;
				else
					cursorTex = verticalCursor;
			}

			Cursor.SetCursor( cursorTex, new Vector2( cursorTex.width * 0.5f, cursorTex.height * 0.5f ), CursorMode.Auto );
		}
	}
}