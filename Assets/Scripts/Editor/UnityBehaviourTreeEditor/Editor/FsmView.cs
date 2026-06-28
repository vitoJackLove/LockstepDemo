using System.Collections;
using System.Collections.Generic;
using TheKiwiCoder;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class FsmView : GraphView
{
    protected override bool canCopySelection => true;

    protected override bool canCutSelection => false; // Cut not supported right now

    protected override bool canPaste => true;

    protected override bool canDuplicateSelection => true;

    protected override bool canDeleteSelection => true;

    public FsmView()
    {
        Insert(0, new GridBackground());
        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new HierarchySelector());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());
    }
    
    
}
