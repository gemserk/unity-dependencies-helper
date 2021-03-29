# Dependencies Helper

WORK IN PROGRESS - Don't use yet.

This is a tool that lets you find dependencies of assets (including subassets) and replace them with another asset.

There are tools that let you find dependencies but they usually work with the full asset not a subasset (if you depend on a subasset it is only tracked as depending on the main asset). This is a problem when using spritesheets as multiple sprites inside a single texture (multisprite) since you don't know which sprite is being used and can make it hard to change one sprite from one spritesheet to another one.

