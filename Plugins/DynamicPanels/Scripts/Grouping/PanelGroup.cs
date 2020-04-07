using System.Collections.Generic;
using UnityEngine;

namespace DynamicPanels
{
	public class PanelGroup : IPanelGroupElement
	{
		internal class InternalSettings
		{
			private readonly PanelGroup group;

			public InternalSettings( PanelGroup group ) { this.group = group; }

			public void SetDirty() { group.SetDirty(); }
			public void UpdateBounds( Vector2 position, Vector2 size ) { group.UpdateBounds( position, size ); }
			public void UpdateLayout() { group.UpdateLayout(); }
			public void UpdateSurroundings( IPanelGroupElement left, IPanelGroupElement top, IPanelGroupElement right, IPanelGroupElement bottom ) { group.UpdateSurroundings( left, top, right, bottom ); }
			public void TryChangeSizeOf( IPanelGroupElement element, Direction direction, float deltaSize ) { group.TryChangeSizeOf( element, direction, deltaSize ); }
			public void ResizeElementTo( IPanelGroupElement element, Vector2 newSize, Direction horizontalDir, Direction verticalDir ) { group.ResizeElementTo( element, newSize, horizontalDir, verticalDir ); }
			public void ReplaceElement( IPanelGroupElement beforeElement, IPanelGroupElement afterElement ) { group.ReplaceElement( beforeElement, afterElement ); }

			public void EnsureMinimumSize()
			{
				for( int i = 0; i < group.elements.Count; i++ )
					group.EnsureMinimumSizeOf( group.elements[i] );
			}
		}

		private class ElementDirtyProperties
		{
			public IPanelGroupElement element;
			public float posX, posY, sizeX, sizeY;

			public ElementDirtyProperties() { }
			public ElementDirtyProperties( IPanelGroupElement element ) { this.element = element; }

			public void Reset( IPanelGroupElement element )
			{
				this.element = element;
				posX = posY = sizeX = sizeY = 0f;
			}
		}

		protected const float MIN_SIZE_TOLERANCE = 1E-4f;

		protected readonly Direction direction;
		protected readonly List<IPanelGroupElement> elements;

		protected readonly IPanelGroupElement[] surroundings;

		public DynamicPanelsCanvas Canvas { get; private set; }
		public PanelGroup Group { get; protected set; }

		internal InternalSettings Internal { get; private set; }

		public Vector2 Position { get; protected set; }
		public Vector2 Size { get; protected set; }
		public Vector2 MinSize { get; protected set; }

		private List<ElementDirtyProperties> resizeProperties;
		private int resizePropsIndex;

		protected bool isDirty = false;

		public int Count { get { return elements.Count; } }
		public IPanelGroupElement this[int index] { get { return elements[index]; } }

		public PanelGroup( DynamicPanelsCanvas canvas, Direction direction )
		{
			Canvas = canvas;
			Internal = new InternalSettings( this );

			this.direction = direction;

			elements = new List<IPanelGroupElement>( 2 );
			surroundings = new IPanelGroupElement[4];
		}

		public bool IsInSameDirection( Direction direction )
		{
			if( direction == Direction.None )
				return false;

			if( direction == Direction.Left || direction == Direction.Right )
				return this.direction == Direction.Left || this.direction == Direction.Right;

			return this.direction == Direction.Top || this.direction == Direction.Bottom;
		}

		public IPanelGroupElement GetSurroundingElement( Direction direction )
		{
			return surroundings[(int) direction];
		}

		protected void SetDirty()
		{
			isDirty = true;

			PanelGroup parentGroup = Group;
			while( parentGroup != null )
			{
				parentGroup.isDirty = true;
				parentGroup = parentGroup.Group;
			}

			Canvas.SetDirty();
		}

		protected virtual void UpdateBounds( Vector2 position, Vector2 size )
		{
			Position = position;

			if( elements.Count == 1 )
				UpdateBoundsOf( elements[0], position, size );
			else
			{
				float multiplier;
				bool horizontal = IsInSameDirection( Direction.Right );
				if( horizontal )
				{
					if( Size.x == 0f )
						multiplier = size.x;
					else
						multiplier = size.x / Size.x;
				}
				else
				{
					if( Size.y == 0f )
						multiplier = size.y;
					else
						multiplier = size.y / Size.y;
				}

				for( int i = 0; i < elements.Count; i++ )
				{
					Vector2 elementSize = elements[i].Size;

					if( horizontal )
					{
						elementSize.x *= multiplier;
						elementSize.y = size.y;

						UpdateBoundsOf( elements[i], position, elementSize );
						position.x += elementSize.x;
					}
					else
					{
						elementSize.x = size.x;
						elementSize.y *= multiplier;

						UpdateBoundsOf( elements[i], position, elementSize );
						position.y += elementSize.y;
					}
				}
			}

			Size = size;
		}

		protected virtual void UpdateLayout()
		{
			if( isDirty )
			{
				elements.RemoveAll( ( element ) => element.IsNull() || element.Group != this );

				for( int i = elements.Count - 1; i >= 0; i-- )
				{
					PanelGroup subGroup = elements[i] as PanelGroup;
					if( subGroup != null )
					{
						subGroup.UpdateLayout();

						int count = subGroup.Count;
						if( count == 0 )
							elements.RemoveAt( i );
						else if( count == 1 )
						{
							elements[i] = subGroup.elements[0];
							SetGroupFor( elements[i], this );
							i++;
						}
						else if( subGroup.IsInSameDirection( direction ) )
						{
							elements.RemoveAt( i );
							elements.InsertRange( i, subGroup.elements );
							for( int j = 0; j < count; j++, i++ )
								SetGroupFor( elements[i], this );
						}
					}
				}

				Vector2 size = Vector2.zero;
				Vector2 minSize = Vector2.zero;
				bool horizontal = IsInSameDirection( Direction.Right );
				int dummyPanelIndex = -1;
				for( int i = 0; i < elements.Count; i++ )
				{
					Vector2 elementSize = elements[i].Size;
					Vector2 elementMinSize = elements[i].MinSize;

					// Rescue elements whose sizes are stuck at 0
					bool rescueElement = false;
					if( elementSize.x == 0f && elementMinSize.x > 0f )
					{
						elementSize.x = Mathf.Min( 1f, elementMinSize.x );
						rescueElement = true;
					}
					if( elementSize.y == 0f && elementMinSize.y > 0f )
					{
						elementSize.y = Mathf.Min( 1f, elementMinSize.y );
						rescueElement = true;
					}

					if( rescueElement )
						UpdateBoundsOf( elements[i], elements[i].Position, elementSize );

					if( i == 0 )
					{
						size = elementSize;
						minSize = elementMinSize;
					}
					else
					{
						if( horizontal )
						{
							size.x += elementSize.x;
							minSize.x += elementMinSize.x;

							if( elementSize.y < size.y )
								size.y = elementSize.y;

							if( elementMinSize.y > minSize.y )
								minSize.y = elementMinSize.y;
						}
						else
						{
							size.y += elementSize.y;
							minSize.y += elementMinSize.y;

							if( elementSize.x < size.x )
								size.x = elementSize.x;

							if( elementMinSize.x > minSize.x )
								minSize.x = elementMinSize.x;
						}
					}

					if( elements[i] is Panel && ( (Panel) elements[i] ).Internal.IsDummy )
						dummyPanelIndex = i;
				}

				if( dummyPanelIndex >= 0 )
				{
					Vector2 flexibleSpace = Vector2.zero;
					if( size.x < Size.x )
					{
						flexibleSpace.x = Size.x - size.x;
						size.x = Size.x;
					}

					if( size.y < Size.y )
					{
						flexibleSpace.y = Size.y - size.y;
						size.y = Size.y;
					}

					( (Panel) elements[dummyPanelIndex] ).RectTransform.sizeDelta += flexibleSpace;
				}

				Size = size;
				MinSize = minSize;

				isDirty = false;
			}
		}

		protected void UpdateSurroundings( IPanelGroupElement left, IPanelGroupElement top, IPanelGroupElement right, IPanelGroupElement bottom )
		{
			surroundings[(int) Direction.Left] = left;
			surroundings[(int) Direction.Top] = top;
			surroundings[(int) Direction.Right] = right;
			surroundings[(int) Direction.Bottom] = bottom;

			bool horizontal = IsInSameDirection( Direction.Right );
			for( int i = 0; i < elements.Count; i++ )
			{
				if( horizontal )
				{
					left = i > 0 ? elements[i - 1] : surroundings[(int) Direction.Left];
					right = i < elements.Count - 1 ? elements[i + 1] : surroundings[(int) Direction.Right];
				}
				else
				{
					bottom = i > 0 ? elements[i - 1] : surroundings[(int) Direction.Bottom];
					top = i < elements.Count - 1 ? elements[i + 1] : surroundings[(int) Direction.Top];
				}

				PanelGroup subGroup = elements[i] as PanelGroup;
				if( subGroup != null )
					subGroup.UpdateSurroundings( left, top, right, bottom );
				else
					( (Panel) elements[i] ).Internal.UpdateSurroundings( left, top, right, bottom );
			}
		}

		protected void ResizeElementTo( IPanelGroupElement element, Vector2 newSize, Direction horizontalDir, Direction verticalDir )
		{
			if( horizontalDir != Direction.Left && horizontalDir != Direction.Right )
				horizontalDir = Direction.Right;
			if( verticalDir != Direction.Bottom && verticalDir != Direction.Top )
				verticalDir = Direction.Bottom;

			Direction horizontalOpposite = horizontalDir.Opposite();
			Direction verticalOpposite = verticalDir.Opposite();

			float flexibleWidth = newSize.x - element.Size.x;
			if( flexibleWidth > MIN_SIZE_TOLERANCE )
			{
				TryChangeSizeOf( element, horizontalDir, flexibleWidth );

				flexibleWidth = newSize.x - element.Size.x;
				if( flexibleWidth > MIN_SIZE_TOLERANCE )
					TryChangeSizeOf( element, horizontalOpposite, flexibleWidth );
			}
			else if( flexibleWidth < -MIN_SIZE_TOLERANCE )
			{
				TryChangeSizeOf( element.GetSurroundingElement( horizontalDir ), horizontalOpposite, -flexibleWidth );

				flexibleWidth = newSize.x - element.Size.x;
				if( flexibleWidth < -MIN_SIZE_TOLERANCE )
					TryChangeSizeOf( element.GetSurroundingElement( horizontalOpposite ), horizontalDir, -flexibleWidth );
			}

			float flexibleHeight = newSize.y - element.Size.y;
			if( flexibleHeight > MIN_SIZE_TOLERANCE )
			{
				TryChangeSizeOf( element, verticalDir, flexibleHeight );

				flexibleHeight = newSize.y - element.Size.y;
				if( flexibleHeight > MIN_SIZE_TOLERANCE )
					TryChangeSizeOf( element, verticalOpposite, flexibleHeight );
			}
			else if( flexibleHeight < -MIN_SIZE_TOLERANCE )
			{
				TryChangeSizeOf( element.GetSurroundingElement( verticalDir ), verticalOpposite, -flexibleHeight );

				flexibleHeight = newSize.y - element.Size.y;
				if( flexibleHeight < -MIN_SIZE_TOLERANCE )
					TryChangeSizeOf( element.GetSurroundingElement( verticalOpposite ), verticalDir, -flexibleHeight );
			}
		}

		protected virtual void EnsureMinimumSizeOf( IPanelGroupElement element )
		{
			float flexibleWidth = element.Size.x - element.MinSize.x;
			if( flexibleWidth < -MIN_SIZE_TOLERANCE )
			{
				TryChangeSizeOf( element, Direction.Right, -flexibleWidth );

				flexibleWidth = element.Size.x - element.MinSize.x;
				if( flexibleWidth < -MIN_SIZE_TOLERANCE )
					TryChangeSizeOf( element, Direction.Left, -flexibleWidth );
			}

			float flexibleHeight = element.Size.y - element.MinSize.y;
			if( flexibleHeight < -MIN_SIZE_TOLERANCE )
			{
				TryChangeSizeOf( element, Direction.Bottom, -flexibleHeight );

				flexibleHeight = element.Size.y - element.MinSize.y;
				if( flexibleHeight < -MIN_SIZE_TOLERANCE )
					TryChangeSizeOf( element, Direction.Top, -flexibleHeight );
			}

			PanelGroup subGroup = element as PanelGroup;
			if( subGroup != null )
				subGroup.Internal.EnsureMinimumSize();
		}

		protected void TryChangeSizeOf( IPanelGroupElement element, Direction direction, float deltaSize )
		{
			if( element.IsNull() || deltaSize <= MIN_SIZE_TOLERANCE || element.GetSurroundingElement( direction ).IsNull() )
				return;

			resizePropsIndex = 0;

			IPanelGroupElement surroundingElement = element.GetSurroundingElement( direction );
			element = surroundingElement.GetSurroundingElement( direction.Opposite() );
			AddResizeProperty( element );

			float deltaMovement = TryChangeSizeOfInternal( surroundingElement, direction, deltaSize );
			if( resizePropsIndex > 1 )
			{
				ResizeElementHelper( 0, direction, deltaMovement );

				for( int i = 0; i < resizePropsIndex; i++ )
				{
					ElementDirtyProperties properties = resizeProperties[i];

					Vector2 position = properties.element.Position + new Vector2( properties.posX, properties.posY );
					Vector2 size = properties.element.Size + new Vector2( properties.sizeX, properties.sizeY );

					UpdateBoundsOf( properties.element, position, size );
				}
			}
		}

		protected float TryChangeSizeOfInternal( IPanelGroupElement element, Direction direction, float deltaSize )
		{
			int currResizePropsIndex = resizePropsIndex;
			AddResizeProperty( element );

			float thisFlexibleSize;
			if( direction == Direction.Left || direction == Direction.Right )
				thisFlexibleSize = element.Size.x - element.MinSize.x;
			else
				thisFlexibleSize = element.Size.y - element.MinSize.y;

			if( thisFlexibleSize > MIN_SIZE_TOLERANCE )
			{
				if( thisFlexibleSize >= deltaSize )
				{
					thisFlexibleSize = deltaSize;
					deltaSize = 0f;
				}
				else
					deltaSize -= thisFlexibleSize;

				ResizeElementHelper( currResizePropsIndex, direction.Opposite(), -thisFlexibleSize );
			}
			else
				thisFlexibleSize = 0f;

			if( deltaSize > MIN_SIZE_TOLERANCE )
			{
				IPanelGroupElement surrounding = element.GetSurroundingElement( direction );
				if( !surrounding.IsNull() )
				{
					if( surrounding.Group != element.Group )
						AddResizeProperty( surrounding.GetSurroundingElement( direction.Opposite() ) );

					float deltaMovement = TryChangeSizeOfInternal( surrounding, direction, deltaSize );
					if( deltaMovement > MIN_SIZE_TOLERANCE )
					{
						if( surrounding.Group == element.Group )
						{
							if( direction == Direction.Left )
								resizeProperties[currResizePropsIndex].posX -= deltaMovement;
							else if( direction == Direction.Top )
								resizeProperties[currResizePropsIndex].posY += deltaMovement;
							else if( direction == Direction.Right )
								resizeProperties[currResizePropsIndex].posX += deltaMovement;
							else
								resizeProperties[currResizePropsIndex].posY -= deltaMovement;

							thisFlexibleSize += deltaMovement;
						}
						else
							ResizeElementHelper( currResizePropsIndex + 1, direction, deltaMovement );
					}
					else
					{
						if( thisFlexibleSize == 0f )
							resizePropsIndex = currResizePropsIndex;
						else
							resizePropsIndex = currResizePropsIndex + 1;
					}
				}
				else if( thisFlexibleSize == 0f )
					resizePropsIndex = currResizePropsIndex;
			}

			return thisFlexibleSize;
		}

		private void AddResizeProperty( IPanelGroupElement element )
		{
			if( resizeProperties == null )
				resizeProperties = new List<ElementDirtyProperties>() { new ElementDirtyProperties( element ), new ElementDirtyProperties() };
			else if( resizePropsIndex == resizeProperties.Count )
				resizeProperties.Add( new ElementDirtyProperties( element ) );
			else
				resizeProperties[resizePropsIndex].Reset( element );

			resizePropsIndex++;
		}

		private void ResizeElementHelper( int resizePropsIndex, Direction direction, float deltaSize )
		{
			ElementDirtyProperties properties = resizeProperties[resizePropsIndex];

			if( direction == Direction.Left )
			{
				properties.posX -= deltaSize;
				properties.sizeX += deltaSize;
			}
			else if( direction == Direction.Top )
				properties.sizeY += deltaSize;
			else if( direction == Direction.Right )
				properties.sizeX += deltaSize;
			else
			{
				properties.posY -= deltaSize;
				properties.sizeY += deltaSize;
			}
		}

		protected void ReplaceElement( IPanelGroupElement beforeElement, IPanelGroupElement afterElement )
		{
			if( beforeElement == afterElement )
				return;

			if( beforeElement.IsNull() || afterElement.IsNull() )
			{
				Debug.LogError( "Invalid argument!" );
				return;
			}

			int index = elements.IndexOf( beforeElement );
			if( index < 0 )
			{
				Debug.LogError( "Invalid index!" );
				return;
			}

			if( beforeElement.Group == this )
				Canvas.UnanchoredPanelGroup.AddElement( beforeElement );

			AddElementAt( index, afterElement );
		}

		public void ResizeTo( Vector2 newSize, Direction horizontalDir = Direction.Right, Direction verticalDir = Direction.Bottom )
		{
			if( Group != null )
				Group.ResizeElementTo( this, newSize, horizontalDir, verticalDir );
		}

		public void DockToRoot( Direction direction )
		{
			PanelManager.Instance.AnchorPanel( this, Canvas, direction );
		}

		public void DockToPanel( IPanelGroupElement anchor, Direction direction )
		{
			PanelManager.Instance.AnchorPanel( this, anchor, direction );
		}

		public void AddElement( IPanelGroupElement element )
		{
			AddElementAt( elements.Count, element );
		}

		public void AddElementBefore( IPanelGroupElement pivot, IPanelGroupElement element )
		{
			AddElementAt( elements.IndexOf( pivot ), element );
		}

		public void AddElementAfter( IPanelGroupElement pivot, IPanelGroupElement element )
		{
			AddElementAt( elements.IndexOf( pivot ) + 1, element );
		}

		protected void AddElementAt( int index, IPanelGroupElement element )
		{
			if( element.IsNull() )
			{
				Debug.LogError( "Invalid argument!" );
				return;
			}

			if( index < 0 || index > elements.Count )
			{
				Debug.LogError( "Invalid index!" );
				return;
			}

			int elementIndex = elements.IndexOf( element );
			if( elementIndex >= 0 && element.Group != this )
			{
				if( index > elementIndex )
					index--;

				elements.RemoveAt( elementIndex );
				elementIndex = -1;
			}

			if( elementIndex == index )
				return;

			if( element.Group != null )
				element.Group.SetDirty();

			if( elementIndex < 0 )
			{
				// Element not present in this group, add it
				elements.Insert( index, element );
				SetGroupFor( element, this );
			}
			else if( elementIndex != index )
			{
				// Element already present in this group, just change its index
				if( elementIndex > index )
					elementIndex++;

				elements.Insert( index, element );
				elements.RemoveAt( elementIndex );
			}

			SetDirty();
		}

		protected void SetGroupFor( IPanelGroupElement element, PanelGroup group )
		{
			Panel panel = element as Panel;
			if( panel != null )
			{
				panel.Internal.Group = group;

				if( panel.RectTransform.parent != group.Canvas.RectTransform )
					panel.RectTransform.SetParent( group.Canvas.RectTransform, false );
			}
			else
				( (PanelGroup) element ).Group = group;
		}

		protected void UpdateBoundsOf( IPanelGroupElement element, Vector2 position, Vector2 size )
		{
			if( element is Panel )
				( (Panel) element ).Internal.UpdateBounds( position, size );
			else
				( (PanelGroup) element ).UpdateBounds( position, size );
		}

		public override string ToString()
		{
			if( direction == Direction.Left || direction == Direction.Right )
				return "Horizontal Group";

			return "Vertical Group";
		}

		// Debug function to print the current hierarchy of groups to console
		public void PrintHierarchy()
		{
			Debug.Log( ToTree( 0, new System.Text.StringBuilder( 500 ) ) );
		}

		private string ToTree( int depth, System.Text.StringBuilder treeBuilder )
		{
			string prefix = string.Empty;
			for( int i = 0; i <= depth; i++ )
				prefix += "-";

			treeBuilder.Append( depth ).Append( prefix ).Append( ' ' ).Append( this ).Append( System.Environment.NewLine );

			foreach( var element in elements )
			{
				if( element is Panel )
					treeBuilder.Append( depth + 1 ).Append( prefix ).Append( "- " ).Append( element ).Append( System.Environment.NewLine );
				else
					( (PanelGroup) element ).ToTree( depth + 1, treeBuilder );
			}

			return depth == 0 ? treeBuilder.ToString() : null;
		}
	}
}