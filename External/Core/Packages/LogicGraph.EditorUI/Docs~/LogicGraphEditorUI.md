# Logic Graph Editor UI

![Overview](resources/overview.png)

Editor UI Package for Logic Graph (Visual Scripting) package.

UI Consists of 3 main parts:
* Graph View - displays nodes and connections between them.
* Blackboard - Holds and allows creation of constant value variables which can be used in the graph view.
* Inspector - Displays the details of the selected node.

## Graph View

![Graph View](resources/graphView.png)

All nodes are displayed and manipulated here. 
Currently there are 3 main types of nodes:

* Group nodes - which allow to nest other nodes inside of them, they can be renamed and recolored.
	* To add a node to a group, drag it on the group container.
	* To remove node from a group, hold shift and drag the node out of a group.
* Comment nodes - yellow nodes which simply hold text. Can be resized.
* Nodes with ports which can be connected to each other, they hold all the logic of the graph.
	* Ports have two types:
		* Flow - white ports which control graph execution order. 
		* Data - hold data which can be accessed by other nodes or scripts. There are 6 supported port data types:
			* bool - red.
			* int - cyan.
			* float - green.
			* string - pink.
			* Vector3 - yellow.
			* Object - grey.

### Searcher - Node creation
![Searcher](resources/searcher.png)
Nodes are created by right clicking on an empty space and selecting needed node in a newly opened search window:

### Minimap
Minimap can be toggled here:  
![Minimap](resources/minimap.png)

### Additional controls

* Pressing F focusses all graph nodes.

## Blackboard

  ![Blackboar](resources/blackboard.png)  
  Holds and allows creation of constant value variables which can be used in the graph view.
 
 Variable can be created by click "+" button, after which a name, type and value can be assigned.
 To create a node instance of a variable, simply drag and drop it on the Graph View. Multiple node instances of a variable can be created.
 Changing variable automatically changes its nodes.

## Inspector

 ![Inspector](resources/inspector.png)
 
Displays details of selected node.

If the node source is MonoBehaviour, its inspector can be seen and edited.
If its source is Custom Event Node, node outputs can be customised:  
 ![Custom event](resources/customNode.png)