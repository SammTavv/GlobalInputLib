# GlobalInputLib
C# library that handles and simulates Windows input globally.

## How to use
Download the `.dll` file and import it in your project.<br>
Add `using GlobalInputLib;` at the beginning of a file to reference the library.

## Methods
The static `Input` class holds every method in this library.<br>
- `KeyDown(VirtualKey vk)`: presses a key
- `KeyUp(VirtualKey vk)`: releases a key
- `MouseLeftDown()`: presses the left mouse button
- `MouseLeftUp()`: releases the left mouse button
- `MouseRightDown()`: presses the right mouse button
- `MouseRightUp()`: releases the right mouse button
- `MouseMiddleDown()`: presses the middle mouse button
- `MouseMiddleUp()`: releases the middle mouse button
- `MouseScrollVertical(int amount)`: scrolls the mouse vertically (default is 120)
- `MouseScrollHorizontal(int amount)`: scrolls the mouse horizontally (default is 120)
- `IsKeyDown(VirtualKey vk)`: checks if a key is pressed
- `KeyTap(VirtualKey vk)`: presses and instantly releases a key
- `ReleaseKeys()`: releases every key
- `MousePosition`: returns the mouse position
