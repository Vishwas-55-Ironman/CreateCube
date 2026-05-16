using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System.Threading;

namespace CreateCube
{
    ///
    /// Manages SolidWorks Application lifecycle
    /// 

    internal sealed class SolidWorksApplicatonManager
    {
        private static SldWorks _swApp;

        public static SldWorks GetApplicationAsync()
        {
            if (_swApp != null)
                return _swApp;

            if (_swApp == null)
            {

                _swApp = Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application")) as SldWorks;

                if (_swApp == null)
                    throw new InvalidOperationException("Failed to Create SolidWorks Application Instance");

                _swApp.Visible = true;
            }

            return _swApp;
        }

        // Exit SolidWorks 
        public static void ExitApplication()
        {
            if (_swApp != null)
            {
                _swApp.ExitApp();
                _swApp = null;
            }
        }

        public static void CreatePart()
        {
            if (_swApp == null)
                throw new ArgumentException(nameof(_swApp));

            string partTemplatepath = _swApp.GetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swDefaultTemplatePart);

            ModelDoc2 swModel = null;

            if (!string.IsNullOrWhiteSpace(partTemplatepath))
            {
                swModel = _swApp.NewDocument(partTemplatepath, 0, 0, 0) as ModelDoc2;
            }
            else
            {
                swModel = _swApp.ActiveDoc;
            }

            if (swModel == null)
                throw new InvalidOperationException("Failed to create a new part. Check SolidWorks Default template settings");

            swModel.Visible = true;
            swModel.ForceRebuild3(false);

            ModelDocExtension swExt = swModel.Extension;
            if (swExt == null)
                throw new InvalidOperationException("Failed to get ModelDocExtension from the document");

            SketchManager skManager = swModel.SketchManager;
            if (skManager == null)
                throw new InvalidOperationException("Failed to get SketchManager from the document");

            bool selFPlane = swExt.SelectByID2("Front Plane", "PLANE", 0, 0, 0, false, 0, null, 0);

            skManager.InsertSketch(true);

            SketchSlot swSketchSlot;
            swSketchSlot = skManager.CreateSketchSlot((int)swSketchSlotCreationType_e.swSketchSlotCreationType_line, (int)swSketchSlotLengthType_e.swSketchSlotLengthType_CenterCenter,
                0.05, -0.05, 0, 0, 0.05, 0, 0, 0, 0, 0, 1, false);

            if (swSketchSlot == null)
                throw new InvalidOperationException("Failed to create sketch slot");

            swModel.ClearSelection2(true);

            // Select and dimension the slot
            Entity slotEntity = swSketchSlot as Entity;
            if (slotEntity != null)
            {
                slotEntity.Select2(false, -1);

                // Add dimensions to slot using constraint menu
                AddSlotDimensions(swModel, swExt);
            }

            // Exit sketch editing mode
            skManager.InsertSketch(false);
        }

        public static void AddSlotDimensions()
        {
            if (_swApp == null)
                throw new ArgumentException(nameof(_swApp));

            ModelDoc2 swModel = _swApp.ActiveDoc;
            if (swModel == null)
                throw new InvalidOperationException("No active SolidWorks document found");

            ModelDocExtension swExt = swModel.Extension;
            if (swExt == null)
                throw new InvalidOperationException("Failed to get ModelDocExtension from the document");

            AddSlotDimensions(swModel, swExt);
        }

        public static void AddSlotDimensions(ModelDoc2 swModel, ModelDocExtension swExt)
        {
            try
            {
                swModel.ClearSelection2(true);

                // Select slot edge and add dimension
                // Dimension 1: Horizontal constraint (type 0)
                swExt.SelectByID2("", "SKETCHSEGMENT", 0.05, -0.05, 0.0, true, 0, null, 0);
                Dimension dim1 = swExt.AddDimension(0.025, -0.1, 0.0, 0);
                if (dim1 != null)
                {
                    dim1.Value = 0.05;
                }

                swModel.ClearSelection2(true);

                // Dimension 2: Vertical constraint (type 1)
                swExt.SelectByID2("", "SKETCHSEGMENT", 0.0, 0.05, 0.0, true, 0, null, 0);
                Dimension dim2 = swExt.AddDimension(0.15, 0.025, 0.0, 1);
                if (dim2 != null)
                {
                    dim2.Value = 0.02;
                }
            }
            catch (Exception ex)
            {
                // Silently continue if dimensioning fails - slot is already created
                System.Diagnostics.Debug.WriteLine("Warning: Could not add dimensions: " + ex.Message);
            }
        }
    }
}

