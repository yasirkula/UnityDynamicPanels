= Dynamic Panels =

Online documentation & example code available at: https://github.com/yasirkula/UnityDynamicPanels
E-mail: yasirkula@gmail.com

### ABOUT
This asset helps you create dynamic panels using Unity's UI system. These panels can be dragged around, resized, docked to canvas edges or to one another and stacked next to each other as separate tabs.


### HOW TO
First, add Dynamic Panels Canvas component to the RectTransform inside which your panels will reside. This RectTransform doesn't have to be the Canvas object itself, it can be a child of the canvas and it can have a custom size.

There are two ways to create panels: by using the GUI of Dynamic Panels Canvas or via Scripting API. There are also two types of panels: free panels that can be moved around and resized freely and docked panels that are moved by the layout system, depending on where it is docked to. A panel can have multiple tabs.

To add a new free panel using the Dynamic Panels Canvas component, simply click the Add New button under the Free Panels section in the Inspector. Then, click the + button to start adding tabs to that panel. Each tab has 4 properties: the content (RectTransform) that will be displayed while the tab is active, a label, an optional icon, and the minimum size of the content associated to the tab. To remove a free panel, select a tab inside the panel and click the Remove Selected button.

You can create docked panels by using the buttons under the Docked Panels section. To create a panel that is docked to the edge of the Dynamic Panels Canvas, use the buttons next to "Dock new panel to canvas:". You can click a panel inside the preview zone (immediately under the Docked Panels section) and edit its tabs. You can also dock a panel to the selected panel using the buttons next to "Dock new panel inside:".

When you are done, click the Play button to see the magic happen!

There are a couple of settings in Dynamic Panels Canvas that you may want to tweak:

- Leave Free Space: when enabled, there will always be some free space in the canvas that docked panels can't fill. Otherwise, docked panels will fill the whole canvas
- Minimum Free Space: if Leave Free Space is enabled, this value will determine the minimum free space
- Free Space Target Transform: if Leave Free Space is enabled and a RectTransform is assigned to this variable, the RectTransform's properties will automatically be updated to always match the free space's position and size
- Prevent Detaching Last Docked Panel: when enabled, trying to detach the last docked panel from the canvas will automatically fail
- Panel Resizable Area Length: size of the invisible resize zones at each side of the panels that allow users to resize the panels
- Canvas Anchor Zone Length: size of the Dynamic Panels Canvas' drop zones. When a tab is dragged and dropped onto that area, it will be docked to that edge of the Dynamic Panels Canvas
- Panel Anchor Zone Length: size of the panels' drop zones. When a tab is dragged and dropped onto that area, it will be docked to that panel. This area is enabled only for docked panels (you can't dock panels to free panels)
- Initial Size: (docked panels only) determines the initial size of a docked panel. This is achieved by programmatically resizing the panel after it is created, so this operation may affect the adjacent panels' sizes, as well. This value won't have any effect if left as (0,0)

NOTE: if you change the Resources/DynamicPanel.prefab, also make sure that the Panel's Header Height property is equal to the distance between the top of the panel and the bottom of the PanelHeader child object (which holds the tabs at runtime).


### PanelCursorHandler COMPONENT
Adding this component to a GameObject will make the cursor dynamic i.e. its texture will change when it enters a panel's resizable area. Note that this component won't have any effect on Android and iOS.


### NEW INPUT SYSTEM SUPPORT
This plugin supports Unity's new Input System but it requires some manual modifications (if both the legacy and the new input systems are active at the same time, no changes are needed):

- the plugin mustn't be installed as a package, i.e. it must reside inside the Assets folder and not the Packages folder (it can reside inside a subfolder of Assets like Assets/Plugins)
- if Unity 2019.2.5 or earlier is used, add ENABLE_INPUT_SYSTEM compiler directive to "Player Settings/Scripting Define Symbols" (these symbols are platform specific, so if you change the active platform later, you'll have to add the compiler directive again)
- add "Unity.InputSystem" assembly to "DynamicPanels.Runtime" Assembly Definition File's "Assembly Definition References" list