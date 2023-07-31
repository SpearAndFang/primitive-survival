**More Info:**

https://mods.vintagestory.at/primitivesurvival

https://www.vintagestory.at/forums/topic/2399-primitive-survival-traps-fishing-and-other-surprises/


## Developer Tools

### Tree Hollow contents customization, courtesy of JapanHasRice!  

_**Thanks so much for this JapanHasRice.  This is some amazing work you've done here!**_

As of version 3.2.0 of Primitive Survival, tree hollow contents are no longer hardcoded. Not that it's simple to add your own contents, but if you have the intestinal fortitude read this.

#### 1. You will need item/block codes.  

These can be easily identified as follows:
- In the game, choose **Settings - Interface**, and enable **Developer Mode**
- In the game, choose **Settings - Debug**, and enable **Extended Debug Info**

Now when you look up an object in the handbook, the _Page Code:_ will tell you everything you need to know. i.e.
```
Page code:block-rottenlog-stump
Page code:item-primitivesurvival:earthworm
```
Note: when you use that information later in a json file, you will to tidy up/rearrange that page code slightly, like so:
```
block-rottenlog-stump
primitivesurvival:item-earthworm
```


#### 2. You will need to enable the Primitive Survival tree hollow developer tools.  

Edit the modconfig file and set **"TreeHollowsEnableDeveloperTools"** to **true**. With this feature enabled, you will have a few new things at your disposal:

- command **/hollow** - look at the ground a couple of blocks in front of you and use this command to place a grown and stocked tree hollow in front of you.  I recommend you have one that's facing north/south, and a second one that's facing east/west for testing purposes.

- command **.hollowtfedit** - when you are looking at a tree hollow that has something in it, this command will allow you to reposition/rotate/resize that object so it looks good in a hollow.  Once it looks good, _Copy JSON_ and paste that code to some temporary location.  Then _Close & Apply_ so you can test your transform in a tree hollow that is oriented differently.

- to test your new transform in hollows that are facing other directions, simply pick up that object and place it in another hollow.  This is the other feature provided by the developer tools.  Normally you cannot place things in grown hollows. 

You have an item/block code and a transform, so you now have everything required to add something new to a hollow.


#### 3. For Primitive Survival and vanilla objects, edit:
>primitivesurvival/blocktypes/wood/treehollowgrown.json

- locate the _treeHollowContentsByHollowType_ key
	- the _base_ section is for items that can appear only at the base of the tree.
	- the _up_ section is for items that only appear higher up in a tree.
	- the _all_ section is for items that can appear in any tree hollow.
	
- Add your new object to one or more of those sections, i.e.
```
{
	"code": "primitivesurvival:item-earthworm",
	"amount": 1
},
```
Note: If the object is from some other mod, look at this file for an example of how that object is added to hollows:
```primitivesurvival/patches/wildcrafttrees/ps-treehollowcontents.json```


#### 4. When you add contents to a tree hollow, it may not render correctly.  

That's where the transform comes in. For example:
```
{
	"name": "intreeHollowTransform",
	"properties":
	{
		rotation: { x: 13, y: -2, z: 9 },
		origin: { x: 0, y: 0.3, z: 0.65 },
		scale: 1.26
	}
}
```

Currently these transforms only work for ITEMS.  To understand how to add these transforms correctly, see some files that I've already put them to use:

```itemtypes/other/earthwormcastings.json```
An inTreeHollowTransform in action in Primitive Survival.

```patches/ps-survival-hollowtransforms.json```
Other examples for vanilla or any mod that isn't primitive survival.  

Note it's _properties/propertiesByType_ we are patching, NOT _behaviors/behaviorsByType_.



