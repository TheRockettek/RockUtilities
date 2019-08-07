# RockUtilities
Utilities mod for Colony Survival

## Home Management

command name   |description
---------------|-----------
/sethome [name]|Sets the name of a home where you are standing. Defaults to "home" if none provided
/delhome [name]|Deletes a home with the name provded. If you only have one home, it will delete it
/homes         |Lists all your active homes
/home [name]   |Teleports to a specified home. If you only have one home, it will use it

## Miscellaneous

command name   |description
---------------|-----------
/spawn         |Teleports you to spawn
/colonyspawn   |Teleports you to nearest active colony banner
/nearcolony    |Displays colonies that are nearby
/players       |Lists players on the server
/near          |Displays players that are neaeby
/warps         |Lists all warps on a server
/warp [name]   |Teleports to a warp
/addwarp [name]|Adds a warp (requires warp.add permission)
/delwarp [name]|Deletes a warp (requires warp.delete permission)
/tpa [name]    |Requests a teleport to a user
/tpahere [name]|Requests for a user to teleport to you
/tpaccept      |Accepts a teleport request
/tpdeny        |Denies a teleport request

## WorldEdit

commmand                                                     |description
-------------------------------------------------------------|-----------
/toggleeditwand | Toggles if your edit wand should work//pos1|Sets position 1 to your location
//pos2                                                       |Sets position 2 to your location
//hpos1                                                      |Sets position 1 to the block you're looking at
//hpos2                                                      |Sets position 2 to the block you're looking at
//clearhistory                                               |Clears undo history
//fast                                                       |Increases speed of worldedit job creation. Increases speed at cost of performance
//limit <limit>                                              |Changes max size of a selection
//set <blocks>                                               |Sets blocks in a selection
//cut                                                        |Removes all blocks in a selection
//wand                                                       |Gives you a wand
//desel                                                      |Removes current selection
//chunk                                                      |Sets selection to current chunk
//expand <length> [direction]                                |Expands the selection in a given direction
//contract <length> [direction]                              |Contracts the selection in a given direction
//outset <length>                                            |Expands the selection in all directions
//count <block>                                              |Counts the occurnace of a specific block in a selection
//distr                                                      |Returns a list of distribution for each block type in a selection
//replace <from> <to>                                        |Replaces certain blocks to another type in a selection
//replacenear <radius> <from> <to>                           |Replaces certain blocks to another type in a radius arround the player
//walls <blocks>                                             |Creates walls along the edges of a selection
//faces <blocks>                                             |Creates walls, ceiling and floor for a selection
//outline <blocks>                                           |Creates an outline for a selection
//hollow <length>                                            |Hollows the inside of a selection
//cyl <blocks> <radius|x,z> [height]                         |Creates a filled cylinder with either a radius or specified width, depth and optionally a height
//hcyl <blocks> <radius|x,z> [height]                        |Creates a hollow cylinder with either a radius or specified width, depth and optionally a height
//sphere <blocks> <radius|x,y,z>                             |Creates a filled sphere with either a radius or specified width, depth and height
//hsphere <blocks> <radius|x,y,z>                            |Creates a hollow sphere with either a radius or specified width, depth and height

### Using RandomBlocks
It is possible to specify multiple block types when <blocks> is used. It will treat the occurance of a specific block as multiple which means that specifying multiple instances of the same block will make it occur more. To be able to specify multiple blocks, they must be spaced with a comma. An example of this distribution is with `stonebricks,stonebricks,stonebricks,stonebrickswhite` which has 3 occurances of stonebricks and one occurance of stonebrickswhite which means that ~75% of blocks placed will be stonebricks and 25% being stonebrickswhite.

## Planned features

- [ ] Teleport to colony and home UI
- [x] Global warp functionality
- [ ] Display a list of colonies
- [ ] Display more colony stats for owners
- [x] WorldEdit
- [ ] WorldGuard-esque functionality
- [ ] GriefLogger (Something similar to CoreProtect)

## Found a bug or want to suggest more features?

You can either PR it or contact me on discord at ImRock#0001
