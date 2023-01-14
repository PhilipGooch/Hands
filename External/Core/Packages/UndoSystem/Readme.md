# Undo system

A system to manage undo/redo operations on editors in a modular and generalistic style.

# How it works
The system has these basic elements:

- **IUndoState** : contains data for an specific part of the current undo step and the context and method to revert it. For example in a text editor one state could be the new string you're writing. This implements a method to undo data too for ease of use.
- **UndoStep** : contains multiple states for an specific action. For example in the text editor example context this could contain 3 states: selection, cursor and new text. This is internal.
- **IUndoStateCollector** : creates a new state and provides it the required data and context. 
- **UndoSystem**: this instanced object keeps everything together. Here is where we call the main methods **RecordUndo()**, **Undo()** and **Redo()**.

To summarize when we **record undo** the **UndoSystem** collects all the **states** through the registered **state collectors**. We can register as many state collectors as we want.

# How to use
## Initial sample
We've a counter class that we want to make undoable
```csharp
    class Counter 
    {
        public int value = 0;

        public void AddOne()
        {
            value++;
        }
    }
```
## Create the state
The state needs context to reapply the previous state with the stored data.
```csharp
    class CounterState : IUndoState
    {
        private readonly Counter counter;
        private int value;

        public CounterState(Counter counter)
        {
            this.counter = counter;
            value = counter.value;
        }

        public void Undo()
        {
            counter.value = value;
        }
    }
```
## Implement state collectors 
The next in the list is implementing state collectors, we're gonna use the Counter class itself since it has everything we need to create the state.
 ```csharp
     class Counter : IUndoStateCollector
    {
        public int value = 0;

        public void AddOne()
        {
            value++;
        }

        public IUndoState RecordUndoState()
        {
            return new CounterState(this);
        }
    }
```
## Initialize
You need to create and store an instance of the undo system with the max number of steps.
```csharp
    int maxUndoSteps = 500;
    UndoSystem undoSystem = new UndoSystem(maxUndoSteps);
```

## Start and register the state collectors
You need to create and store an instance of the undo system with the max number of steps.    
```csharp
    Counter counter1 = new Counter();
    Counter counter2 = new Counter();
    undoSystem.StartSystem(counter1,counter2);
```
The order is preserved during operations... so if we undo it's going to undo **counter1** first and then **counter2**.
## Recording undo
Calling this method will create a new undo step:
```csharp
    undoSystem.RecordUndo();
```
But in the case you need to update the latest undo step you can use:
```csharp
    undoSystem.OverwriteUndo();
```
You can add this inside the operations that you perform that modify data to keep track of everything:
```csharp
    public void AddOne()
    {
        value++;
        undoSystem.RecordUndo();
    }
```
## Undo Redo
This is the easiest part:
```csharp
    undoSystem.Undo();
    undoSystem.Redo();
```
