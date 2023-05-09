**More Info:**

https://mods.vintagestory.at/primitivesurvival

https://www.vintagestory.at/forums/topic/2399-primitive-survival-traps-fishing-and-other-surprises/


### Developer Tools: Tree Hollow contents customization, courtesy of JapanHasRice!

Tree hollow contents are no longer hardcoded. Not that it's simple to add your own contents, but if you have the intestinal fortitude, read this.

#### 1. You will need item/block codes.  These can be easily identified as follows:
- In the game, choose **Settings - Interface**, and enable **Developer Mode**
- In the game, choose **Settings - Debug**, and enable **Extended Debug Info**

Now when you look up an object in the handbook, the _Page Code:_ will tell you everything you need to know. i.e.
```
Page code:block-rottenlog-stump
Page code:item-primitivesurvival:earthworm
```
Note: when you use that information later in a json file, you will rearrange that page code slightly, like so:
```
block-rottenlog-stump
primitivesurvival:item-earthworm
```

#### 2. You will need to enable the Primitive Survival tree hollow developer tools.  Edit the modconfig file and set **"TreeHollowsEnableDeveloperTools"** to true.

With this feature enabled, you will have a few new things at your disposal:

- command **/hollow** - look at the ground a couple of blocks in front of you and use this command to place a GROWN tree hollow in front of you.  I recommend you have one that's facing north/south, and a second one that's facing east/west for testing purposes.

- command **.hollowtfedit** - when you are looking at a tree hollow that has something in it, this command will allow you to reposition/rotate/resize that object so it looks good in a hollow.  Once it looks good, "Copy JSON" and paste that code to some temporary location.  Then "Close & Apply" so you can test your transform in a tree hollow that is oriented differently.

- to test your new transform in hollows that are facing other directions, simply pick up that object and place it in another hollow.  This is the other feature provided by the developer tools.  Normally you cannot place things in grown hollows. 

You have an item/block code and a transform, so you now have everything required to add something new to a hollow.


#### 3. For Primitive Survival and vanilla objects, edit
>primitivesurvival/blocktypes/wood/treehollowgrown.json

- locate the _treeHollowContentsByHollowType_ key
	- the _base_ section is for items that can appear only at the base of the tree
	- the _up_ section is for items that only appear higher up in a tree
	- the _all_ section is for items that can appear in any tree hollow
	
- Add your new object to one or more of those sections, i.e.
```
{
	"code": "primitivesurvival:item-earthworm",
	"amount": 1
},
```
Note: If the object is from some other mod, look at this file for an example of how that object is added to hollows:
>primitivesurvival/patches/wildcrafttrees/ps-treehollowcontents.json


#### 4. When you add contents to a tree hollow, it may not render correctly.  That's what the transform info is for. For examples of this in action, see:

>itemtypes/other/earthwormcastings.json
inTreeHollowTransform in action in Primitive Survival

>patches/ps-survival-hollowtransforms.json
other examples for vanilla or any mod that isn't primitive survival.  Note it's propertiesByType we are patching, NOT behaviorsByType.


