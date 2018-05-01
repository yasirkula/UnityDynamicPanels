using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DynamicPanels
{
	[DisallowMultipleComponent]
	public class PanelTab : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		private Panel m_panel;
		public Panel Panel { get { return m_panel; } }

		public RectTransform RectTransform { get; private set; }

		public RectTransform Content { get; private set; }

		public Vector2 MinSize { get; private set; }

		[SerializeField]
		private Image background;

		[SerializeField]
		private Image iconHolder;

		[SerializeField]
		private Text nameHolder;

		public Sprite Icon
		{
			get { return iconHolder != null ? iconHolder.sprite : null; }
			set
			{
				if( iconHolder != null )
				{
					iconHolder.gameObject.SetActive( value != null );
					iconHolder.sprite = value;
				}
			}
		}

		public string Label
		{
			get { return nameHolder != null ? nameHolder.text : null; }
			set
			{
				if( nameHolder != null && value != null )
					nameHolder.text = value;
			}
		}

		private int pointerId = PanelManager.NON_EXISTING_TOUCH;

		public bool IsBeingDetached { get { return pointerId != PanelManager.NON_EXISTING_TOUCH; } }

		private void Awake()
		{
			RectTransform = (RectTransform) transform;
			MinSize = new Vector2( 100f, 100f );

			iconHolder.preserveAspect = true;
        }

		private void OnEnable()
		{
			pointerId = PanelManager.NON_EXISTING_TOUCH;
		}

		public void Initialize( Panel panel, RectTransform content )
		{
			m_panel = panel;
			Content = content;
		}

		public void SetMinSize( Vector2 minSize )
		{
			MinSize = minSize;
		}

		public void SetActive( bool activeState )
		{
			if( Content == null || Content.Equals( null ) )
				m_panel.Internal.RemoveTab( m_panel.Internal.GetTabIndex( this ), true );
			else
			{
				if( activeState )
					background.color = m_panel.TabSelectedColor;
				else
					background.color = m_panel.TabNormalColor;

				Content.gameObject.SetActive( activeState );
			}
		}

		public void OnPointerClick( PointerEventData eventData )
		{
			if( Content == null || Content.Equals( null ) )
				m_panel.Internal.RemoveTab( m_panel.Internal.GetTabIndex( this ), true );
			else
				m_panel.ActiveTab = m_panel.Internal.GetTabIndex( this );
		}

		public void OnBeginDrag( PointerEventData eventData )
		{
			// Cancel drag event if panel is already being dragged by another pointer,
			// or PanelManager does not want the panel to be dragged at that moment
			if( !PanelManager.Instance.OnBeginPanelTabTranslate( this, eventData ) )
			{
				eventData.pointerDrag = null;
				return;
			}

			pointerId = eventData.pointerId;
			background.color = m_panel.TabDetachingColor;
        }

		public void OnDrag( PointerEventData eventData )
		{
			if( eventData.pointerId != pointerId )
			{
				eventData.pointerDrag = null;
				return;
			}

			PanelManager.Instance.OnPanelTabTranslate( this, eventData );
        }

		public void OnEndDrag( PointerEventData eventData )
		{
			if( eventData.pointerId != pointerId )
				return;

			pointerId = PanelManager.NON_EXISTING_TOUCH;
			ResetBackgroundColor();

			PanelManager.Instance.OnEndPanelTabTranslate( this, eventData );
		}

		public void Stop()
		{
			if( pointerId != PanelManager.NON_EXISTING_TOUCH )
			{
				ResetBackgroundColor();
				pointerId = PanelManager.NON_EXISTING_TOUCH;
			}
		}

		private void ResetBackgroundColor()
		{
			if( m_panel.ActiveTab == m_panel.Internal.GetTabIndex( this ) )
				background.color = m_panel.TabSelectedColor;
			else
				background.color = m_panel.TabNormalColor;
		}
	}
}