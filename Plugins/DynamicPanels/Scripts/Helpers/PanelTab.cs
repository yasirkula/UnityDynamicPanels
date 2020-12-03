using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DynamicPanels
{
	[DisallowMultipleComponent]
	public class PanelTab : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		internal class InternalSettings
		{
			private readonly PanelTab tab;
			public readonly RectTransform RectTransform;

			public InternalSettings( PanelTab tab )
			{
				this.tab = tab;
				RectTransform = (RectTransform) tab.transform;
			}

			public bool IsBeingDetached { get { return tab.pointerId != PanelManager.NON_EXISTING_TOUCH; } }

			public void Initialize( Panel panel, RectTransform content )
			{
				tab.m_panel = panel;
				tab.Content = content;
			}

			public void ChangeCloseButtonVisibility( bool isVisible )
			{
				if( tab.closeButton && isVisible != tab.closeButton.gameObject.activeSelf )
				{
					tab.closeButton.gameObject.SetActive( isVisible );

					float tabSizeDelta = tab.closeButton.GetComponent<LayoutElement>().preferredWidth;
					tab.GetComponent<LayoutElement>().preferredWidth += isVisible ? tabSizeDelta : -tabSizeDelta;
				}
			}

			public void Stop()
			{
				if( tab.pointerId != PanelManager.NON_EXISTING_TOUCH )
				{
					tab.ResetBackgroundColor();
					tab.pointerId = PanelManager.NON_EXISTING_TOUCH;

					PanelNotificationCenter.Internal.TabDragStateChanged( tab, false );
				}
			}

			public void SetActive( bool activeState ) { tab.SetActive( activeState ); }
		}

#pragma warning disable 0649
		[SerializeField]
		private Image background;

		[SerializeField]
		private Image iconHolder;

		[SerializeField]
		private Text nameHolder;

		[SerializeField]
		private Button closeButton;
#pragma warning restore 0649

		internal InternalSettings Internal { get; private set; }

		private string m_id = null;
		public string ID
		{
			get { return m_id; }
			set
			{
				if( !string.IsNullOrEmpty( value ) && m_id != value )
				{
					PanelNotificationCenter.Internal.TabIDChanged( this, m_id, value );
					m_id = value;
				}
			}
		}

		private Panel m_panel;
		public Panel Panel { get { return m_panel; } }

		public int Index
		{
			get { return m_panel.GetTabIndex( this ); }
			set { m_panel.AddTab( this, value ); }
		}

		public RectTransform Content { get; private set; }

		private Vector2 m_minSize;
		public Vector2 MinSize
		{
			get { return m_minSize; }
			set
			{
				if( m_minSize != value )
				{
					m_minSize = value;
					m_panel.Internal.RecalculateMinSize();
				}
			}
		}

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

		private void Awake()
		{
			m_minSize = new Vector2( 100f, 100f );
			Internal = new InternalSettings( this );

			iconHolder.preserveAspect = true;

			closeButton.onClick.AddListener( () => PanelNotificationCenter.Internal.TabClosed( this ) );
		}

		private void Start()
		{
			if( string.IsNullOrEmpty( m_id ) )
				ID = System.Guid.NewGuid().ToString();
		}

		private void OnEnable()
		{
			pointerId = PanelManager.NON_EXISTING_TOUCH;
		}

		private void OnDestroy()
		{
			PanelNotificationCenter.Internal.TabIDChanged( this, m_id, null );
		}

		public void AttachTo( Panel panel, int tabIndex = -1 )
		{
			panel.AddTab( Content, tabIndex );
		}

		public Panel Detach()
		{
			return m_panel.DetachTab( this );
		}

		public void Destroy()
		{
			m_panel.RemoveTab( this );
		}

		private void SetActive( bool activeState )
		{
			if( !Content )
				m_panel.Internal.RemoveTab( m_panel.GetTabIndex( this ), true );
			else
			{
				if( activeState )
				{
					background.color = m_panel.TabSelectedColor;
					nameHolder.color = m_panel.TabSelectedTextColor;
				}
				else
				{
					background.color = m_panel.TabNormalColor;
					nameHolder.color = m_panel.TabNormalTextColor;
				}

				Content.gameObject.SetActive( activeState );
			}
		}

		void IPointerClickHandler.OnPointerClick( PointerEventData eventData )
		{
			if( !Content )
				m_panel.Internal.RemoveTab( m_panel.GetTabIndex( this ), true );
			else
				m_panel.ActiveTab = m_panel.GetTabIndex( this );
		}

		void IBeginDragHandler.OnBeginDrag( PointerEventData eventData )
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
			nameHolder.color = m_panel.TabDetachingTextColor;

			PanelNotificationCenter.Internal.TabDragStateChanged( this, true );
		}

		void IDragHandler.OnDrag( PointerEventData eventData )
		{
			if( eventData.pointerId != pointerId )
			{
				eventData.pointerDrag = null;
				return;
			}

			PanelManager.Instance.OnPanelTabTranslate( this, eventData );
		}

		void IEndDragHandler.OnEndDrag( PointerEventData eventData )
		{
			if( eventData.pointerId != pointerId )
				return;

			pointerId = PanelManager.NON_EXISTING_TOUCH;
			ResetBackgroundColor();

			PanelManager.Instance.OnEndPanelTabTranslate( this, eventData );
			PanelNotificationCenter.Internal.TabDragStateChanged( this, false );
		}

		private void ResetBackgroundColor()
		{
			if( m_panel.ActiveTab == m_panel.GetTabIndex( this ) )
			{
				background.color = m_panel.TabSelectedColor;
				nameHolder.color = m_panel.TabSelectedTextColor;
			}
			else
			{
				background.color = m_panel.TabNormalColor;
				nameHolder.color = m_panel.TabNormalTextColor;
			}
		}
	}
}