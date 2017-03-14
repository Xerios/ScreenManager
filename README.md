# ScreenManager
Flexible way to manage your UI with transitions, layers and proper navigation handling for Unity.

## Features
* ScreenEditor inspector, but the whole system can be used fully through code
* Optimized clean and bloatfree code, no additional libraries required
* Button-mashing-proof, screens respect their order and animate in/out properly
* **Spread** feature: See all your screens in a grid view for an easier overview and editing
* Customizable generic screen class
  * Including pre-made presets: Animate, Tween, AnimationController
  * Easily create your own type of screen using the BaseScreen class
* Multi-layered stack based navigation _( alertbox > popup > main )_
  * Re-usable screen instances
  * Duplication of screens _( popups, alerts )_
* Uses default input handling using Unity's UI events _( including cancel/back event )_
* **Buttons navigation generation** using Unity's internal UI navigation system while preventing from navigation bleeding out to other screens
* Always-on selected button for gamepad and keyboard-aware navigation
* Mobile tested
* Includes an example
* Fully documented code

## Gif

![Gif](/img/animated.gif?raw=true "Gif")

_( Note: The inspector window is not forcefully updated, which is why it lags a bit behind )_

## Screenshots

![Screenshot](/img/extension.png?raw=true "Screenshot")
![Screenshot](/img/animatorscreen.png?raw=true "Screenshot")
![Screenshot](/img/screen.png?raw=true "Screenshot")
![Screenshot](/img/spread.png?raw=true "Screenshot")

# Sample code
````
screenmgr.Show("MainMenu");
screenmgr.ShowPopup<Popup>("Alertbox").Message = "Custom Alert Text";
screenmgr.HideAll();
````

## How to use ?
Simply create a new ScreenManager on any canvas element. 
(Preferably right underneath the main canvas element.)

Ignore the ScreenManager settings for now, let's create some screens.

To start, we can add SimpleTweenScreen under ScreenManager GameObject.

For custom screens: You can either inherit `BaseScreen` class or any other pre-made classes: _AnimatorScreen, Popup, SimpleTweenScreen, TweenedScreen_

Once **SimpleTweenScreen** is added, you'll notice few things :
* It added a CanvasGroup element
* It has a lot of strange settings

By default `BaseScreen` class provides several important settings, these settings are the core of makes the screen work so well :

* Generation Settings
 * **Generate Navigation** ( Create explicit navigation so that keyboard/gamepad navigation never leaks to an another screen )
 * **Cancel Selection** ( Button selected or executed when ESC or back button is pressed )
* Cancel Selection
 * **Hide Current** ( Hide previous screen or overlay on top of the previous screen )
 * **Keep on Top when hiding** ( Keep its drawing order on top when hiding )
 * **Layer Priority** ( All screens have layers, imagine that an alert box will block all screen changes that are below it but wouldn't block other alert boxes )
 * **Default Selection** ( Button selected by default when the screen is shown, when left empty : first button found is selected )

You can leave those settings as they are for now, no need for additional configuration.


Once we added **SimpleTweenScreen** to all our screens, we can define our default screen that's going to be open on run.

Go to **ScreenManager** _( Shortcut Ctrl+Alt+T or Window/Select ScreenManager in the menu )_

Select your **SimpleTweenScreen** in the Screens section. 
Once selected you'll see a button named "Set Default" appear. 

Click on it, it will set that screen as the default/main screen.

![ScreenManager](/img/step0.png?raw=true "ScreenManager")

Now that's done we can pass to setting up our buttons.

There's two ways to setup the buttons :

![Button](/img/step1.png?raw=true "Button")
![Button](/img/step2.png?raw=true "Button")

You can either show a screen by its name, or by it's GameObject reference.


( Tutorial to be finished )

## Contribution
There's always more to add, so if you want to help then feel free to contribute !
