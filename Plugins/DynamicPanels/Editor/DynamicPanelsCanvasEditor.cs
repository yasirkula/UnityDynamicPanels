using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DynamicPanels
{
	[CustomEditor( typeof( DynamicPanelsCanvas ) )]
	[CanEditMultipleObjects]
	public class DynamicPanelsCanvasEditor : Editor
	{
		private DynamicPanelsCanvas.InternalSettings settings;

		private const float LABEL_WIDTH = 100f;
		private const float ANCHORED_PANELS_PREVIEW_HEIGHT = 350f;
		private const string SHOW_IDS_PREF = "DynamicPanels_ShowIDs";

		private static bool showIDs;

		private List<ReorderableList> reorderableLists;
		private int reorderableListIndex;
		private bool isReorderableListSelected;

		private DynamicPanelsCanvas.AnchoredPanelProperties selectedAnchoredPanel;
		private DynamicPanelsCanvas.AnchoredPanelProperties justClickedAnchoredPanel;
		private List<DynamicPanelsCanvas.PanelTabProperties> selectedAnchoredPanelTabs;

		private GUIStyle anchoredPanelGUIStyle;

		private SerializedProperty leaveFreeSpace;
		private SerializedProperty minimumFreeSpace;
		private SerializedProperty freeSpaceTargetTransform;
		private SerializedProperty preventDetachingLastDockedPanel;
		private SerializedProperty panelResizableAreaLength;
		private SerializedProperty canvasAnchorZoneLength;
		private SerializedProperty panelAnchorZoneLength;

		private void OnEnable()
		{
			settings = ( (DynamicPanelsCanvas) target ).Internal;

			reorderableLists = new List<ReorderableList>();
			selectedAnchoredPanel = settings.InitialPanelsAnchored;
			selectedAnchoredPanelTabs = selectedAnchoredPanel.panel.tabs;

			leaveFreeSpace = serializedObject.FindProperty( "m_leaveFreeSpace" );
			minimumFreeSpace = serializedObject.FindProperty( "minimumFreeSpace" );
			freeSpaceTargetTransform = serializedObject.FindProperty( "freeSpaceTargetTransform" );
			preventDetachingLastDockedPanel = serializedObject.FindProperty( "PreventDetachingLastDockedPanel" );
			panelResizableAreaLength = serializedObject.FindProperty( "m_panelResizableAreaLength" );
			canvasAnchorZoneLength = serializedObject.FindProperty( "m_canvasAnchorZoneLength" );
			panelAnchorZoneLength = serializedObject.FindProperty( "m_panelAnchorZoneLength" );

			showIDs = EditorPrefs.GetBool( SHOW_IDS_PREF, false );

			Undo.undoRedoPerformed -= OnUndo;
			Undo.undoRedoPerformed += OnUndo;
		}

		private void OnUndo()
		{
			settings = ( (DynamicPanelsCanvas) target ).Internal;

			selectedAnchoredPanel = settings.InitialPanelsAnchored;
			selectedAnchoredPanelTabs = selectedAnchoredPanel.panel.tabs;
		}

		[MenuItem( "CONTEXT/DynamicPanelsCanvas/Toggle Show IDs" )]
		private static void ToggleShowIDs()
		{
			showIDs = !EditorPrefs.GetBool( SHOW_IDS_PREF, false );
			EditorPrefs.SetBool( SHOW_IDS_PREF, showIDs );
		}

		public override void OnInspectorGUI()
		{
			if( anchoredPanelGUIStyle == null )
			{
				anchoredPanelGUIStyle = new GUIStyle( "box" )
				{
					alignment = TextAnchor.MiddleCenter,
					clipping = TextClipping.Clip
				};
			}

			if( justClickedAnchoredPanel != null && Event.current.type == EventType.Layout )
			{
				selectedAnchoredPanel = justClickedAnchoredPanel;
				selectedAnchoredPanelTabs = selectedAnchoredPanel.panel.tabs;

				justClickedAnchoredPanel = null;
			}

			serializedObject.Update();
			reorderableListIndex = 0;

			bool multiObjectEditing = targets.Length > 1;
			bool guiEnabled = !EditorApplication.isPlaying || AssetDatabase.Contains( ( (DynamicPanelsCanvas) serializedObject.targetObject ).gameObject );

			GUI.enabled = guiEnabled;
			GUILayout.BeginVertical();

			EditorGUILayout.LabelField( "= Properties =", EditorStyles.boldLabel );
			EditorGUILayout.PropertyField( leaveFreeSpace );
			EditorGUI.indentLevel++;
			GUI.enabled = guiEnabled && leaveFreeSpace.boolValue;
			EditorGUILayout.PropertyField( minimumFreeSpace );
			EditorGUILayout.PropertyField( freeSpaceTargetTransform );
			GUI.enabled = guiEnabled;
			EditorGUI.indentLevel--;
			EditorGUILayout.PropertyField( preventDetachingLastDockedPanel );
			EditorGUILayout.PropertyField( panelResizableAreaLength );
			EditorGUILayout.PropertyField( canvasAnchorZoneLength );
			EditorGUILayout.PropertyField( panelAnchorZoneLength );

			GUILayout.Space( 10f );

			EditorGUILayout.LabelField( "= Free Panels =", EditorStyles.boldLabel );
			if( multiObjectEditing )
				GUILayout.Label( "Multi-object editing not supported" );
			else if( !guiEnabled )
				GUILayout.Label( "Can't edit in Play mode" );
			else
			{
				List<DynamicPanelsCanvas.PanelProperties> initialPanelsUnanchored = settings.InitialPanelsUnanchored;
				int selectedReorderableListIndex = -1;
				for( int i = 0; i < initialPanelsUnanchored.Count; i++ )
				{
					if( DrawReorderableListFor( initialPanelsUnanchored[i] ) )
						selectedReorderableListIndex = i;

					if( i < initialPanelsUnanchored.Count - 1 )
					{
						// Draw a horizontal line to separate the panels
						GUILayout.Space( 5f );
						GUILayout.Box( GUIContent.none, GUILayout.ExpandWidth( true ), GUILayout.Height( 2f ) );
					}

					GUILayout.Space( 5f );
				}

				GUILayout.BeginHorizontal();

				if( GUILayout.Button( "Add New", GUILayout.Height( 1.35f * EditorGUIUtility.singleLineHeight ) ) )
				{
					Undo.IncrementCurrentGroup();
					Undo.RecordObject( (DynamicPanelsCanvas) target, "Add Free Panel" );

					initialPanelsUnanchored.Add( new DynamicPanelsCanvas.PanelProperties() );
				}

				if( selectedReorderableListIndex < 0 )
					GUI.enabled = false;

				if( GUILayout.Button( "Remove Selected", GUILayout.Height( 1.35f * EditorGUIUtility.singleLineHeight ) ) )
				{
					Undo.IncrementCurrentGroup();
					Undo.RecordObject( (DynamicPanelsCanvas) target, "Remove Free Panel" );

					initialPanelsUnanchored.RemoveAt( selectedReorderableListIndex );
				}

				GUI.enabled = guiEnabled;

				GUILayout.EndHorizontal();
			}

			GUILayout.Space( 13f );

			EditorGUILayout.LabelField( "= Docked Panels =", EditorStyles.boldLabel );
			if( multiObjectEditing )
				GUILayout.Label( "Multi-object editing not supported" );
			else if( !guiEnabled )
				GUILayout.Label( "Can't edit in Play mode" );
			else
			{
				DynamicPanelsCanvas.AnchoredPanelProperties initialPanelsAnchored = settings.InitialPanelsAnchored;

				Rect previewRect = EditorGUILayout.GetControlRect( false, ANCHORED_PANELS_PREVIEW_HEIGHT );
				DrawAnchoredPanelsPreview( previewRect, initialPanelsAnchored );

				if( selectedAnchoredPanel != null )
				{
					GUILayout.BeginVertical( EditorStyles.helpBox );
					GUILayout.Space( 5f );
					EditorGUILayout.LabelField( "Selected panel:", EditorStyles.boldLabel );

					if( selectedAnchoredPanelTabs != settings.InitialPanelsAnchored.panel.tabs )
					{
						string initialSizeLabel = selectedAnchoredPanel.initialSize == Vector2.zero ? "Initial Size (not set):" : "Initial Size:";

						EditorGUI.BeginChangeCheck();
						Vector2 panelInitialSize = EditorGUILayout.Vector2Field( initialSizeLabel, selectedAnchoredPanel.initialSize );
						if( EditorGUI.EndChangeCheck() )
						{
							Undo.RecordObject( (DynamicPanelsCanvas) target, "Change Initial Size" );
							selectedAnchoredPanel.initialSize = panelInitialSize;
						}

						DrawReorderableListFor( selectedAnchoredPanel.panel );
					}
					else
						GUILayout.Label( "- nothing -" );

					Direction direction = ShowDirectionButtons( "Dock new panel inside: " );
					if( direction != Direction.None )
					{
						Undo.IncrementCurrentGroup();
						Undo.RecordObject( (DynamicPanelsCanvas) target, "Dock New Panel" );

						selectedAnchoredPanel.subPanels.Add( new DynamicPanelsCanvas.AnchoredPanelProperties() { anchorDirection = direction } );
					}

					if( selectedAnchoredPanelTabs != settings.InitialPanelsAnchored.panel.tabs )
					{
						GUILayout.Space( 5f );
						if( GUILayout.Button( "Remove Selected", GUILayout.Height( 1.35f * EditorGUIUtility.singleLineHeight ) ) )
							RemoveAnchoredPanel( settings.InitialPanelsAnchored, selectedAnchoredPanel );
					}

					GUILayout.EndVertical();
				}

				GUILayout.Space( 6f );

				Direction rootDirection = ShowDirectionButtons( "Dock new panel to canvas: " );
				if( rootDirection != Direction.None )
				{
					Undo.IncrementCurrentGroup();
					Undo.RecordObject( (DynamicPanelsCanvas) target, "Dock New Panel" );

					settings.InitialPanelsAnchored.subPanels.Insert( 0, new DynamicPanelsCanvas.AnchoredPanelProperties() { anchorDirection = rootDirection } );
				}
			}

			GUI.enabled = true;
			GUILayout.EndVertical();

			serializedObject.ApplyModifiedProperties();
		}

		private Direction ShowDirectionButtons( string label )
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label( label );

			Direction result = Direction.None;
			if( GUILayout.Button( "Left" ) )
				result = Direction.Left;
			if( GUILayout.Button( "Top" ) )
				result = Direction.Top;
			if( GUILayout.Button( "Right" ) )
				result = Direction.Right;
			if( GUILayout.Button( "Bottom" ) )
				result = Direction.Bottom;

			GUILayout.EndHorizontal();

			return result;
		}

		private bool RemoveAnchoredPanel( DynamicPanelsCanvas.AnchoredPanelProperties root, DynamicPanelsCanvas.AnchoredPanelProperties panel )
		{
			for( int i = 0; i < root.subPanels.Count; i++ )
			{
				if( root.subPanels[i] == panel )
				{
					Undo.IncrementCurrentGroup();
					Undo.RecordObject( (DynamicPanelsCanvas) target, "Remove Panel" );

					DynamicPanelsCanvas.AnchoredPanelProperties replacementPanel = null;
					if( panel.subPanels.Count > 0 )
					{
						replacementPanel = panel.subPanels[panel.subPanels.Count - 1];
						panel.subPanels.RemoveAt( panel.subPanels.Count - 1 );

						if( replacementPanel != null )
						{
							replacementPanel.anchorDirection = panel.anchorDirection;
							replacementPanel.subPanels.InsertRange( 0, panel.subPanels );
						}
					}

					if( replacementPanel == null )
						root.subPanels.RemoveAt( i );
					else
						root.subPanels[i] = replacementPanel;

					justClickedAnchoredPanel = settings.InitialPanelsAnchored;
					return true;
				}
				else if( RemoveAnchoredPanel( root.subPanels[i], panel ) )
					return true;
			}

			return false;
		}

		private void DrawAnchoredPanelsPreview( Rect rect, DynamicPanelsCanvas.AnchoredPanelProperties props )
		{
			bool shouldDrawSelf = leaveFreeSpace.boolValue || props != settings.InitialPanelsAnchored || props.subPanels == null || props.subPanels.Count == 0;
			if( props.subPanels != null && props.subPanels.Count > 0 )
			{
				int horizontal = 1, vertical = 1;
				for( int i = 0; i < props.subPanels.Count; i++ )
				{
					Direction anchorDirection = props.subPanels[i].anchorDirection;
					if( anchorDirection == Direction.Left || anchorDirection == Direction.Right )
						horizontal++;
					else
						vertical++;
				}

				if( !shouldDrawSelf )
				{
					Direction anchorDirection = props.subPanels[props.subPanels.Count - 1].anchorDirection;
					if( anchorDirection == Direction.Left || anchorDirection == Direction.Right )
					{
						if( horizontal > 1 )
							horizontal--;
					}
					else
					{
						if( vertical > 1 )
							vertical--;
					}
				}

				float perWidth = rect.width / horizontal;
				float perHeight = rect.height / vertical;
				for( int i = 0; i < props.subPanels.Count; i++ )
				{
					Rect subRect = new Rect( rect );
					Direction anchorDirection = props.subPanels[i].anchorDirection;
					if( anchorDirection == Direction.Left )
					{
						rect.x += perWidth;
						rect.width -= perWidth;
						subRect.width = perWidth;
					}
					else if( anchorDirection == Direction.Top )
					{
						rect.y += perHeight;
						rect.height -= perHeight;
						subRect.height = perHeight;
					}
					else if( anchorDirection == Direction.Right )
					{
						rect.width -= perWidth;
						subRect.width = perWidth;
						subRect.x = rect.xMax;
					}
					else
					{
						rect.height -= perHeight;
						subRect.height = perHeight;
						subRect.y = rect.yMax;
					}

					DrawAnchoredPanelsPreview( subRect, props.subPanels[i] );
				}
			}

			if( !shouldDrawSelf )
				return;

			string label;
			if( props == settings.InitialPanelsAnchored )
				label = "Free space";
			else
			{
				label = "Panel";

				List<DynamicPanelsCanvas.PanelTabProperties> tabs = props.panel.tabs;
				for( int i = 0; i < tabs.Count; i++ )
				{
					if( !string.IsNullOrEmpty( tabs[i].tabLabel ) )
					{
						label = tabs[i].tabLabel;
						break;
					}
				}

				if( tabs.Count == 1 )
					label = string.Concat( label, "\n1 tab" );
				else
					label = string.Concat( label, "\n", tabs.Count.ToString(), " tabs" );
			}

			if( selectedAnchoredPanel == props )
			{
				Color guiColor = GUI.color;
				GUI.color = Color.cyan;
				GUI.Box( rect, label, anchoredPanelGUIStyle );
				GUI.color = guiColor;
			}
			else
				GUI.Box( rect, label, anchoredPanelGUIStyle );

			int controlID = GUIUtility.GetControlID( FocusType.Passive );

			Event ev = Event.current;
			switch( ev.GetTypeForControl( controlID ) )
			{
				case EventType.MouseDown:
					if( rect.Contains( ev.mousePosition ) && ev.button == 0 )
					{
						GUIUtility.hotControl = controlID;
						justClickedAnchoredPanel = props;
					}

					break;
				case EventType.MouseDrag:
					if( GUIUtility.hotControl == controlID && props != settings.InitialPanelsAnchored )
					{
						GUIUtility.hotControl = 0;

						// Credit: https://forum.unity.com/threads/editor-draganddrop-bug-system-needs-to-be-initialized-by-unity.219342/#post-1464056
						DragAndDrop.PrepareStartDrag();
						DragAndDrop.objectReferences = new Object[] { null };
						DragAndDrop.SetGenericData( "props", props );
						DragAndDrop.StartDrag( "AnchoredPanelProperties" );

						ev.Use();
					}

					break;
				case EventType.MouseUp:
					if( GUIUtility.hotControl == controlID )
						GUIUtility.hotControl = 0;

					break;
				case EventType.DragPerform:
				case EventType.DragUpdated:
					if( props != settings.InitialPanelsAnchored && rect.Contains( ev.mousePosition ) )
					{
						DynamicPanelsCanvas.AnchoredPanelProperties drag = DragAndDrop.GetGenericData( "props" ) as DynamicPanelsCanvas.AnchoredPanelProperties;
						if( drag == null )
						{
							int i;
							Object[] draggedObjects = DragAndDrop.objectReferences;
							for( i = 0; i < draggedObjects.Length; i++ )
							{
								if( draggedObjects[i] is GameObject || draggedObjects[i] is Component )
									break;
							}

							if( i == draggedObjects.Length )
								break;
						}

						DragAndDrop.visualMode = DragAndDropVisualMode.Move;
						if( ev.type == EventType.DragPerform )
						{
							DragAndDrop.AcceptDrag();

							if( drag != null )
							{
								Undo.IncrementCurrentGroup();
								Undo.RecordObject( (DynamicPanelsCanvas) target, "Swap Tabs" );

								DynamicPanelsCanvas.PanelProperties temp = props.panel;
								props.panel = drag.panel;
								drag.panel = temp;
							}
							else
							{
								Undo.IncrementCurrentGroup();
								Undo.RecordObject( (DynamicPanelsCanvas) target, "Add Tabs" );

								Object[] draggedObjects = DragAndDrop.objectReferences;
								for( int i = 0; i < draggedObjects.Length; i++ )
								{
									RectTransform transform;
									if( draggedObjects[i] is GameObject )
										transform = ( (GameObject) draggedObjects[i] ).transform as RectTransform;
									else
										transform = ( (Component) draggedObjects[i] ).transform as RectTransform;

									if( transform != null )
									{
										if( props.panel.tabs.Find( ( tab ) => tab.content == transform ) == null )
											props.panel.tabs.Add( new DynamicPanelsCanvas.PanelTabProperties() { content = transform } );
									}
								}
							}
						}

						ev.Use();
					}

					break;
			}

			if( ev.isMouse && GUIUtility.hotControl == controlID )
				ev.Use();
		}

		private bool DrawReorderableListFor( DynamicPanelsCanvas.PanelProperties panelProperties )
		{
			isReorderableListSelected = false;
			float elementHeight = ( showIDs ? 4 : 3 ) * EditorGUIUtility.singleLineHeight + 2;

			List<DynamicPanelsCanvas.PanelTabProperties> tabs = panelProperties.tabs;
			if( reorderableLists.Count > reorderableListIndex )
			{
				reorderableLists[reorderableListIndex].list = tabs;
				reorderableLists[reorderableListIndex].elementHeight = elementHeight;
			}
			else
			{
				ReorderableList reorderableList = new ReorderableList( tabs, typeof( DynamicPanelsCanvas.PanelTabProperties ), true, true, true, true )
				{
					elementHeight = elementHeight,
					onAddCallback = ( thisList ) =>
					{
						Undo.IncrementCurrentGroup();
						Undo.RecordObject( (DynamicPanelsCanvas) target, "Add Tab" );

						ReorderableList.defaultBehaviours.DoAddButton( thisList );
					},
					onRemoveCallback = ( thisList ) =>
					{
						Undo.IncrementCurrentGroup();
						Undo.RecordObject( (DynamicPanelsCanvas) target, "Remove Tab" );

						ReorderableList.defaultBehaviours.DoRemoveButton( thisList );
					},
					drawHeaderCallback = ( rect ) => GUI.Label( rect, "Tabs" )
				};

				reorderableList.drawElementCallback = ( Rect rect, int index, bool isActive, bool isFocused ) =>
				{
					isReorderableListSelected |= isActive && isFocused;
					DrawReorderableListItem( rect, index, (DynamicPanelsCanvas.PanelTabProperties) reorderableList.list[index] );
				};

				reorderableLists.Add( reorderableList );
			}

			reorderableLists[reorderableListIndex].DoLayoutList();
			reorderableListIndex++;

			return isReorderableListSelected;
		}

		private void DrawReorderableListItem( Rect rect, int index, DynamicPanelsCanvas.PanelTabProperties tab )
		{
			rect.y += 2;

			float lineHeight = EditorGUIUtility.singleLineHeight;
			float contentWidth = rect.width - LABEL_WIDTH;
			Vector2 tabMinimumSize = tab.minimumSize;

			Rect contentLabelRect = new Rect( rect.x, rect.y, LABEL_WIDTH, lineHeight );
			Rect contentRect = new Rect( rect.x + LABEL_WIDTH, rect.y, contentWidth, lineHeight );

			Rect tabMetadataLabelRect = new Rect( rect.x, rect.y + lineHeight, LABEL_WIDTH, lineHeight );
			Rect tabLabelRect = new Rect( rect.x + LABEL_WIDTH, rect.y + lineHeight, contentWidth * 0.5f, lineHeight );
			Rect tabIconRect = new Rect( rect.x + LABEL_WIDTH + contentWidth * 0.5f, rect.y + lineHeight, contentWidth * 0.5f, lineHeight );

			Rect minSizeLabelRect = new Rect( rect.x, rect.y + 2 * lineHeight, LABEL_WIDTH, lineHeight );
			Rect minSizeXRect = new Rect( rect.x + LABEL_WIDTH, rect.y + 2 * lineHeight, contentWidth * 0.5f, lineHeight );
			Rect minSizeYRect = new Rect( rect.x + LABEL_WIDTH + contentWidth * 0.5f, rect.y + 2 * lineHeight, contentWidth * 0.5f, lineHeight );

			Rect idLabelRect = new Rect( rect.x, rect.y + 3 * lineHeight, LABEL_WIDTH, lineHeight );
			Rect idRect = new Rect( rect.x + LABEL_WIDTH, rect.y + 3 * lineHeight, contentWidth, lineHeight );

			GUI.Label( contentLabelRect, "Content:" );
			EditorGUI.BeginChangeCheck();
			RectTransform content = EditorGUI.ObjectField( contentRect, GUIContent.none, tab.content, typeof( RectTransform ), true ) as RectTransform;
			if( EditorGUI.EndChangeCheck() )
			{
				Undo.RecordObject( (DynamicPanelsCanvas) target, "Change Content" );
				tab.content = content;
			}

			GUI.Label( tabMetadataLabelRect, "Label/Icon:" );
			EditorGUI.BeginChangeCheck();
			string tabLabel = EditorGUI.TextField( tabLabelRect, GUIContent.none, tab.tabLabel );
			if( EditorGUI.EndChangeCheck() )
			{
				Undo.RecordObject( (DynamicPanelsCanvas) target, "Change Label" );
				tab.tabLabel = tabLabel;
			}

			EditorGUI.BeginChangeCheck();
			Sprite tabIcon = EditorGUI.ObjectField( tabIconRect, GUIContent.none, tab.tabIcon, typeof( Sprite ), false ) as Sprite;
			if( EditorGUI.EndChangeCheck() )
			{
				Undo.RecordObject( (DynamicPanelsCanvas) target, "Change Icon" );
				tab.tabIcon = tabIcon;
			}

			GUI.Label( minSizeLabelRect, "Min Size (XY):" );
			EditorGUI.BeginChangeCheck();
			tabMinimumSize.x = EditorGUI.FloatField( minSizeXRect, GUIContent.none, tabMinimumSize.x );
			tabMinimumSize.y = EditorGUI.FloatField( minSizeYRect, GUIContent.none, tabMinimumSize.y );
			if( EditorGUI.EndChangeCheck() )
			{
				Undo.RecordObject( (DynamicPanelsCanvas) target, "Change Minimum Size" );
				tab.minimumSize = tabMinimumSize;
			}

			if( showIDs )
			{
				GUI.Label( idLabelRect, "ID:" );
				EditorGUI.BeginChangeCheck();
				string tabID = EditorGUI.TextField( idRect, GUIContent.none, tab.id );
				if( EditorGUI.EndChangeCheck() )
				{
					Undo.RecordObject( (DynamicPanelsCanvas) target, "Change ID" );
					tab.id = tabID;
				}
			}
		}
	}
}