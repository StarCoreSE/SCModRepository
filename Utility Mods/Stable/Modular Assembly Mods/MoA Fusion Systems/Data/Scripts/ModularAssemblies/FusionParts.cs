﻿using System.Collections.Generic;
using StarCore.FusionSystems.FusionParts;
using VRageMath;
using static StarCore.FusionSystems.Communication.DefinitionDefs;

namespace StarCore.FusionSystems
{
    internal partial class ModularDefinition
    {
        // You can declare functions in here, and they are shared between all other ModularDefinition files.

        // This is the important bit.
        private ModularPhysicalDefinition ModularFusion => new ModularPhysicalDefinition
        {
            // Unique name of the definition.
            Name = "Modular_Fusion",

            OnInit = () => { SFusionManager.I.FusionDefinition = this; },

            // Triggers whenever a new part is added to an assembly.
            OnPartAdd = SFusionManager.I.OnPartAdd,

            // Triggers whenever a part is removed from an assembly.
            OnPartRemove = SFusionManager.I.OnPartRemove,

            // Triggers whenever a part is destroyed, simultaneously with OnPartRemove
            OnPartDestroy = (physicalAssemblyId, blockEntity, isBaseBlock) =>
            {
                // You can remove this function, and any others if need be.
            },

            // The most important block in an assembly. Connection checking starts here.
            BaseBlockSubtype = null,

            // All SubtypeIds that can be part of this assembly.
            AllowedBlockSubtypes = new[]
            {
                "Caster_FocusLens",
                "Caster_Accelerator_0",
                "Caster_Accelerator_90",
                "Caster_CentralPipe_0",
                "Caster_CentralPipe_90",
                "Caster_CentralPipe_T",
                "Caster_Feeder",
                //"Caster_Controller",
                "Caster_Reactor",
                "Caster_ConveyorCap",
            },

            // Allowed connection directions & whitelists, measured in blocks.
            // If an allowed SubtypeId is not included here, connections are allowed on all sides.
            // If the connection type whitelist is empty, all allowed subtypes may connect on that side.
            AllowedConnections = new Dictionary<string, Dictionary<Vector3I, string[]>>
            {
                {
                    // Note - Offsets line up with BuildInfo block orientation.
                    // Note - Offsets are measured from the center of the block; in this case, the Caster_FocusLens is a 3x3 that has connections on the back in a plus shape.
                    "Caster_FocusLens", new Dictionary<Vector3I, string[]>
                    {
                        {
                            new Vector3I(1, 0, 2), new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                "Caster_Reactor",
                                "Caster_Controller",
                                "Caster_FocusLens",
                                "Caster_ConveyorCap"
                            }
                        },
                        {
                            new Vector3I(-1, 0, 2), new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                "Caster_Reactor",
                                "Caster_Controller",
                                "Caster_FocusLens",
                                "Caster_ConveyorCap"
                            }
                        },
                        {
                            new Vector3I(0, 1, 2), new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                "Caster_Reactor",
                                "Caster_Controller",
                                "Caster_FocusLens",
                                "Caster_ConveyorCap"
                            }
                        },
                        {
                            new Vector3I(0, -1, 2), new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                "Caster_Reactor",
                                "Caster_Controller",
                                "Caster_FocusLens",
                                "Caster_ConveyorCap"
                            }
                        }
                    }
                },
                {
                    "Caster_Reactor", new Dictionary<Vector3I, string[]>
                    {
                        {
                            new Vector3I(0, 2, 0), new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                "Caster_FocusLens",
                                //"Caster_Controller",
                                "Caster_Reactor",
                                "Caster_ConveyorCap"
                            }
                        },
                        {
                            new Vector3I(0, -2, 0), new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                "Caster_FocusLens",
                                //"Caster_Controller",
                                "Caster_Reactor",
                                "Caster_ConveyorCap"
                            }
                        }
                    }
                },
                {
                    "Caster_Accelerator_0", new Dictionary<Vector3I, string[]>
                    {
                        {
                            Vector3I.Forward, new[]
                            {
                                "Caster_Accelerator_0",
                                "Caster_Accelerator_90",
                                "Caster_Feeder"
                            }
                        },
                        {
                            Vector3I.Backward, new[]
                            {
                                "Caster_Accelerator_0",
                                "Caster_Accelerator_90",
                                "Caster_Feeder"
                            }
                        }
                    }
                },
                {
                    "Caster_Accelerator_90", new Dictionary<Vector3I, string[]>
                    {
                        {
                            Vector3I.Forward, new[]
                            {
                                "Caster_Accelerator_0",
                                "Caster_Accelerator_90",
                                "Caster_Feeder"
                            }
                        },
                        {
                            Vector3I.Right, new[]
                            {
                                "Caster_Accelerator_0",
                                "Caster_Accelerator_90",
                                "Caster_Feeder"
                            }
                        }
                    }
                },
                {
                    "Caster_CentralPipe_0", new Dictionary<Vector3I, string[]>
                    {
                        {
                            Vector3I.Forward, new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                //"Caster_Controller",
                                "Caster_FocusLens",
                                "Caster_Reactor",
                                "Caster_ConveyorCap"
                            }
                        },
                        {
                            Vector3I.Backward, new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                //"Caster_Controller",
                                "Caster_FocusLens",
                                "Caster_Reactor",
                                "Caster_ConveyorCap"
                            }
                        }
                    }
                },
                {
                    "Caster_CentralPipe_90", new Dictionary<Vector3I, string[]>
                    {
                        {
                            Vector3I.Forward, new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                //"Caster_Controller",
                                "Caster_FocusLens",
                                "Caster_Reactor",
                                "Caster_ConveyorCap"
                            }
                        },
                        {
                            Vector3I.Right, new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                //"Caster_Controller",
                                "Caster_FocusLens",
                                "Caster_Reactor",
                                "Caster_ConveyorCap"
                            }
                        }
                    }
                },
                {
                    "Caster_CentralPipe_T", new Dictionary<Vector3I, string[]>
                    {
                        {
                            Vector3I.Forward, new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                //"Caster_Controller",
                                "Caster_FocusLens",
                                "Caster_Reactor",
                                "Caster_ConveyorCap"
                            }
                        },
                        {
                            Vector3I.Right, new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                //"Caster_Controller",
                                "Caster_FocusLens",
                                "Caster_Reactor",
                                "Caster_ConveyorCap"
                            }
                        },
                        {
                            Vector3I.Backward, new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                //"Caster_Controller",
                                "Caster_FocusLens",
                                "Caster_Reactor",
                                "Caster_ConveyorCap"
                            }
                        }
                    }
                },
                {
                    "Caster_Feeder", new Dictionary<Vector3I, string[]>
                    {
                        {
                            Vector3I.Forward, new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                //"Caster_Controller",
                                "Caster_FocusLens",
                                "Caster_Reactor",
                                "Caster_ConveyorCap"
                            }
                        },
                        {
                            Vector3I.Backward, new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                //"Caster_Controller",
                                "Caster_FocusLens",
                                "Caster_Reactor",
                                "Caster_ConveyorCap"
                            }
                        },

                        {
                            Vector3I.Up, new[]
                            {
                                "Caster_Accelerator_0",
                                "Caster_Accelerator_90",
                                "Caster_Feeder"
                            }
                        },
                        {
                            Vector3I.Down, new[]
                            {
                                "Caster_Accelerator_0",
                                "Caster_Accelerator_90",
                                "Caster_Feeder"
                            }
                        }
                    }
                },
                //{
                //    "Caster_Controller", new Dictionary<Vector3I, string[]>
                //    {
                //        {
                //            Vector3I.Backward, new[]
                //            {
                //                "Caster_CentralPipe_0",
                //                "Caster_CentralPipe_90",
                //                "Caster_CentralPipe_T",
                //                "Caster_Feeder",
                //                "Caster_Reactor"
                //            }
                //        }
                //    }
                //}
                {
                    "Caster_ConveyorCap", new Dictionary<Vector3I, string[]>
                    {
                        {
                            Vector3I.Backward, new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                "Caster_Reactor",
                                "Caster_FocusLens",
                            }
                        }
                    }
                }
            }
        };
    }
}