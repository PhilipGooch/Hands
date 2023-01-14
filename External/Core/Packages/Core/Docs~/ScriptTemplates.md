# Script templates

## Default templates

Unity allows you to create numerous text files: C# scripts, shader, assembly definitions etc. The templates for these 
files can be found in: `<Unity installation>\Editor\Data\Resources\ScriptTemplates`

## Creating custom per-project templates

Unity also has undocumented feature of allowing you to create per-project templates. Unity recognizes templates inside `Assets/ScriptTemplates` folder. We have a custom script that collects and copies templates from our custom packages to the said folder. To create custom template you must place them inside `<package root folder>/ScriptTemplates`. And then you should run `No Brakes Games/Development/Import script templates` command.
Script templates have to follow a specific filename format, to be added to the 'Create' context menu:
`MenuOrder–MenuPath–DefaultFileName.FileExtension.txt`. For example the menu item for creating a default MonoBehaviour is encoded like this: `81-C# Script-NewBehaviourScript.cs.txt`

* __MenuOrder__ – the lower the number, the higher template will appear in the context menu.
* __MenuPath__ – path in the context menu in the Create option.
	* To create categories in __MenuPath__ use two underscores as a separator:
	`MenuOrder-CategoryDepth1__CategoryDepth2__ItemName-DefaultFileName.FileExtension.txt`
	
**NOTE: Editor needs to be relaunched after new templates are added to** `Assets/ScriptTemplates` **folder for them to appear in the context menu.**
	
## Formatting within templates

Templates use keywords to replace certain parts of their conent with custom values. Everything except these keywords gets copied as-is.
Some of the keywords:
* `#SCRIPTNAME#` – the file name you entered when creating a file from the template will appear here.
* `#NOTRIM#` – ensures that the whitespace is not deleted.

Since these features are not documented, it's recommended to refer to the default templates when creating a new one.

## Further reading

This readme was prepared using this article: https://4experience.co/how-to-create-useful-unity-script-templates/ the article goes into a bit more depth on this feature.
