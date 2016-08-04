# ScreenManager
Flexible way to manage screens with transitions, layers and proper input handling for Unity.
Fully documented, except for this readme though...

Few screenshots :
![Screenshot](/img/spread.png?raw=true "Screenshot")
![Screenshot](/img/extension.png?raw=true "Screenshot")
![Screenshot](/img/screen.png?raw=true "Screenshot")

## How to use ?
Simply create a new ScreenManager on any canvas element ( preferably right underneath the main canvas element ex : UI > ScreenManager )
We'll look into ScreenManager settings later on. For now we need to create our screens.

Under ScreenManager create all your screens.
Note that all screens are required to have a BaseScreen compatible class ( ex: AnimatorScreen, Popup, SimpleTweenScreen, TweenedScreen )

To start with we can add SimpleTweenScreen, once added you'll notice few things :
* It added a CanvasGroup element
* It has a lot of strange settings

By default BaseScreen provides several important settings :
* Generation Settings
** *Generate Navigation* ( Create explicit navigation so that keyboard/gamepad navigation never links to another/outside screen )
** *Cancel Selection* ( Button selected when ESC or back button is pressed )
* Cancel Selection
** *Hide Current* ( Hide previous screen or overlay on top of the previous screen )
** *Keep on Top when hiding* ( Keep its drawing order on top when hiding )
** *Layer Priority* ( Which layer to use ) _I'm gonna have to explain this further later on_
** *Default Selection* ( Button selected by default when the screen is shown, when left empty : first button found is selected )

You can leave them as they are for now, no need for additional configuration.


Once we added SimpleTweenScreen to all our screens, we can define our default screen that's going to be open on run.

Go to ScreenManager _( You can use ther shortcut Ctrl+Alt+T or Window/Select ScreenManager in the menu )_
Select your Screen in the Screens section and then once selected you'll see a button named "Set Default" will appear. Click on that. 
![ScreenManager](/img/step0.png?raw=true "ScreenManager")

Now that's done we can pass to setting up our buttons.
There's two ways to setup the buttons :
![Button](/img/step1.png?raw=true "Button")
![Button](/img/step2.png?raw=true "Button")

( Documentation to be finished )