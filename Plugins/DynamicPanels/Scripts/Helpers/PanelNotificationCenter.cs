using System.Collections.Generic;

namespace DynamicPanels
{
	public static class PanelNotificationCenter
	{
		public static class Internal
		{
			public static void PanelCreated( Panel panel )
			{
				if( !IsPanelRegistered( panel ) )
				{
					panels.Add( panel );

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
		public static event PanelDelegate OnPanelCreated, OnPanelDestroyed, OnPanelBecameActive, OnPanelBecameInactive;

		private static readonly List<Panel> panels = new List<Panel>( 32 );
		public static int NumberOfPanels { get { return panels.Count; } }

		public static Panel GetPanel( int panelIndex )
		{
			if( panelIndex >= 0 && panelIndex < panels.Count )
				return panels[panelIndex];

			return null;
		}
	}
}