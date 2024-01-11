using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game;
using VRage.ModAPI;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Utils;
using VRageMath;

[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
public class SessionComp : MySessionComponentBase
{
    private bool isPredictionDisabled = true;
    private bool shouldDrawServerLine = false;  // New field
    private MyEntity lastControlledEntity = null;
    private DateTime lastDrawn = DateTime.MinValue;
    private int timer = 0;
    private DateTime lastLineDrawn = DateTime.Now;
    private TimeSpan drawInterval = TimeSpan.FromMilliseconds(30);


    struct LineToDraw
    {
        public Vector3D Origin;
        public Vector3D Direction;
        public DateTime Timestamp;
        public Color Color;


        public LineToDraw(Vector3D origin, Vector3D direction, DateTime timestamp, Color color)
        {
            this.Origin = origin;
            this.Direction = direction;
            this.Timestamp = timestamp;
            this.Color = color;
        }
    }

    private List<LineToDraw> linesToDraw = new List<LineToDraw>();

    public override void LoadData()
    {
        if (!MyAPIGateway.Utilities.IsDedicated)
        {
            MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;
        }
    }

        public override void UpdateAfterSimulation()
        {
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                // Existing code for controlled entities and predictions
                MyEntity controlledEntity = GetControlledGrid();
                MyEntity cockpitEntity = GetControlledCockpit(controlledEntity);

                if (controlledEntity != null && !controlledEntity.Equals(lastControlledEntity))
                {
                    lastControlledEntity = controlledEntity;
                    MyCubeGrid controlled = controlledEntity as MyCubeGrid;

                    if (controlled != null)
                    {
                        controlled.ForceDisablePrediction = isPredictionDisabled;
                        MyAPIGateway.Utilities.ShowNotification($"You are controlling: {controlledEntity.DisplayName}, ForceDisablePrediction: {isPredictionDisabled}", 2000, MyFontEnum.Red);
                    }
                }
                else if (controlledEntity == null)
                {
                    lastControlledEntity = null;
                }
                else if (controlledEntity.Equals(lastControlledEntity))
                {
                    MyCubeGrid controlled = controlledEntity as MyCubeGrid;
                    if (controlled != null)
                    {
                        controlled.ForceDisablePrediction = isPredictionDisabled;
                    }
                }

            // Timer-based line drawing
            timer += 1; // Increment counter since UpdateAfterSimulation is called every millisecond

            if (timer >= 30) // Check if 30ms have passed
            {
                timer = 0; // Reset counter

                if (shouldDrawServerLine && cockpitEntity != null)
                {
                    // Calculate the center of the grid
                    Vector3D gridCenter = (controlledEntity as MyCubeGrid)?.PositionComp.WorldAABB.Center ?? Vector3D.Zero;

                    // Use cockpit's forward direction
                    Vector3D direction = cockpitEntity.WorldMatrix.Forward;

                    // Add the line to draw
                    linesToDraw.Add(new LineToDraw(gridCenter, direction, DateTime.Now, Color.Red));
                }
            }


            // Call DrawLines every update
            DrawLines();
        }
    }

    private MyEntity GetControlledGrid()
    {
        try
        {
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.Player == null)
            {
                return null;
            }

            var controlledEntity = MyAPIGateway.Session.Player.Controller?.ControlledEntity?.Entity;
            if (controlledEntity == null)
            {
                return null;
            }

            if (controlledEntity is IMyCockpit || controlledEntity is IMyRemoteControl)
            {
                return (controlledEntity as IMyCubeBlock).CubeGrid as MyEntity;
            }
        }
        catch (Exception e)
        {
            MyLog.Default.WriteLine($"Error in GetControlledGrid: {e}");
        }

        return null;
    }

    private MyEntity GetControlledCockpit(MyEntity controlledGrid)
    {
        if (controlledGrid == null)
            return null;

        var grid = controlledGrid as MyCubeGrid;
        if (grid == null)
            return null;

        foreach (var block in grid.GetFatBlocks())
        {
            var cockpit = block as MyCockpit; // Convert the block to MyCockpit
            if (cockpit != null)
            {
                if (cockpit.WorldMatrix != null)  // Add null check here
                    return cockpit;
            }
        }
        return null;
    }


    private void OnMessageEntered(string messageText, ref bool sendToOthers)
    {
        if (messageText.Equals("/toggleprediction"))
        {
            isPredictionDisabled = !isPredictionDisabled;
            MyAPIGateway.Utilities.ShowNotification($"ForceDisablePrediction: {isPredictionDisabled}", 2000, MyFontEnum.Red);
            sendToOthers = false;
        }
        else if (messageText.Equals("/toggleserverline"))
        {
            shouldDrawServerLine = !shouldDrawServerLine;  // Toggle flag
            sendToOthers = false;
        }
    }


    private void DrawLines()
    {
        float length = 100f;
        float thickness = 0.25f;

        DateTime now = DateTime.Now;

        // Remove lines older than 2 seconds
        linesToDraw.RemoveAll(line => (now - line.Timestamp).TotalSeconds >= 2);

        // Draw lines
        foreach (var line in linesToDraw)
        {
            // Draw the line if it should be visible
            if ((now - line.Timestamp).TotalSeconds < 2)
            {
                Vector4 colorVector = new Vector4(line.Color.R / 255.0f, line.Color.G / 255.0f, line.Color.B / 255.0f, line.Color.A / 255.0f);
                Vector3D endPoint = line.Origin + line.Direction * length;
                MySimpleObjectDraw.DrawLine(line.Origin, endPoint, MyStringId.GetOrCompute("Square"), ref colorVector, thickness);
            }
        }
    }



    protected override void UnloadData()
    {
        if (!MyAPIGateway.Utilities.IsDedicated)
        {
            MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;
        }
    }
}
