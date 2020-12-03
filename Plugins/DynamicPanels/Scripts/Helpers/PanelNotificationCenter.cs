using System.Collections.Generic;

namespace DynamicPanels
{
	public static class PanelNotificationCenter
	{
		internal static class Internal
		{
			public static void PanelCreated( Panel panel )
			{
				if( !IsPanelRegistered( panel ) )
				{
					panels.Add( panel );

					panel.Internal.ChangeCloseButtonVisibility( m_onPanelClosed != null );

					if( OnPanelCreated != null )
						OnPanelCreated( panel );

					if( panel.gameObject.activeInHierarchy )
					{
						if( OnPanelBecameActive != null )
							OnPanelBecameActive( panel );
					}
					else
					{
						if( OnPanelBecameInactive != null )
							OnPanelBecameInactive( panel );
					}
				}
			}

			public static void PanelDestroyed( Panel panel )
			{
				if( panels.Remove( panel ) && OnPanelDestroyed != null )
					OnPanelDestroyed( panel );
			}

			public static void PanelBecameActive( Panel panel )
			{
				if( IsPanelRegistered( panel ) )
				{
					if( OnPanelBecameActive != null )
						OnPanelBecameActive( panel );
				}
			}

			public static void PanelBecameInactive( Panel panel )
			{
				if( IsPanelRegistered( panel ) )
				{
					if( OnPanelBecameInactive != null )
						OnPanelBecameInactive( panel );
				}
			}

			public static void PanelClosed( Panel panel )
			{
				if( m_onPanelClosed != null )
					m_onPanelClosed( panel );
			}

			public static void TabDragStateChanged( PanelTab tab, bool isDragging )
			{
				if( isDragging )
				{
					if( OnStartedDraggingTab != null )
						OnStartedDraggingTab( tab );
				}
				else
				{
					if( OnStoppedDraggingTab != null )
						OnStoppedDraggingTab( tab );
				}
			}

			public static void ActiveTabChanged( PanelTab tab )
			{
				if( OnActiveTabChanged != null )
					OnActiveTabChanged( tab );
			}

			public static void TabIDChanged( PanelTab tab, string previousID, string newID )
			{
				if( !idToTab.ContainsValue( tab ) )
				{
					tab.Internal.ChangeCloseButtonVisibility( m_onTabClosed != null );

					if( OnTabCreated != null )
						OnTabCreated( tab );
				}

				if( !string.IsNullOrEmpty( previousID ) )
				{
					PanelTab previousTab;
					if( idToTab.TryGetValue( previousID, out previousTab ) && previousTab == tab )
						idToTab.Remove( previousID );
				}

				if( !string.IsNullOrEmpty( newID ) )
					idToTab[newID] = tab;
				else if( OnTabDestroyed != null )
					OnTabDestroyed( tab );
			}

			public static void TabClosed( PanelTab tab )
			{
				if( m_onTabClosed != null )
					m_onTabClosed( tab );
			}

			private static bool IsPanelRegistered( Panel panel )
			{
				for( int i = panels.Count - 1; i >= 0; i-- )
				{
					if( panels[i] == panel )
						return true;
				}

				return false;
			}
		}

		public delegate void PanelDelegate( Panel panel );
		public delegate void TabDelegate( PanelTab tab );

		public static event PanelDelegate OnPanelCreated, OnPanelDestroyed, OnPanelBecameActive, OnPanelBecameInactive;
		public static event TabDelegate OnTabCreated, OnTabDestroyed, OnActiveTabChanged, OnStartedDraggingTab, OnStoppedDraggingTab;

		private static PanelDelegate m_onPanelClosed;
		public static event PanelDelegate OnPanelClosed
		{
			add
			{
				if( value != null )
				{
					if( m_onPanelClosed == null )
					{
						for( int i = panels.Count - 1; i >= 0; i-- )
							panels[i].Internal.ChangeCloseButtonVisibility( true );
					}

					m_onPanelClosed += value;
				}
			}
			remove
			{
				if( value != null && m_onPanelClosed != null )
				{
					m_onPanelClosed -= value;

					if( m_onPanelClosed == null )
					{
						for( int i = panels.Count - 1; i >= 0; i-- )
							panels[i].Internal.ChangeCloseButtonVisibility( false );
					}
				}
			}
		}

		private static TabDelegate m_onTabClosed;
		public static event TabDelegate OnTabClosed
		{
			add
			{
				if( value != null )
				{
					if( m_onTabClosed == null )
					{
						foreach( PanelTab tab in idToTab.Values )
							tab.Internal.ChangeCloseButtonVisibility( true );
					}

					m_onTabClosed += value;
				}
			}
			remove
			{
				if( value != null && m_onTabClosed != null )
				{
					m_onTabClosed -= value;

					if( m_onTabClosed == null )
					{
						foreach( PanelTab tab in idToTab.Values )
							tab.Internal.ChangeCloseButtonVisibility( false );
					}
				}
			}
		}

		private static readonly List<Panel> panels = new List<Panel>( 32 );
		public static int NumberOfPanels { get { return panels.Count; } }

		private static readonly Dictionary<string, PanelTab> idToTab = new Dictionary<string, PanelTab>( 32 );

		public static Panel GetPanel( int panelIndex )
		{
			if( panelIndex >= 0 && panelIndex < panels.Count )
				return panels[panelIndex];

			return null;
		}

		public static bool TryGetTab( string tabID, out PanelTab tab )
		{
			if( string.IsNullOrEmpty( tabID ) )
			{
				tab = null;
				return false;
			}

			return idToTab.TryGetValue( tabID, out tab );
		}
	}
}