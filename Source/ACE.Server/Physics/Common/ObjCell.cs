using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ACE.Entity.Enum;
using ACE.Server.Physics.Animation;
using ACE.Server.Physics.Combat;

namespace ACE.Server.Physics.Common
{
    public class ObjCell: PartCell, IEquatable<ObjCell>
    {
        public uint ID;
        public LandDefs.WaterType WaterType;
        public Position Pos;
        public int NumObjects;
        public List<PhysicsObj> ObjectList;
        public int NumLights;
        public List<int> LightList;
        public int NumShadowObjects;
        public List<ShadowObj> ShadowObjectList;
        public List<uint> ShadowObjectIDs;
        public uint RestrictionObj;
        public List<int> ClipPlanes;
        public int NumStabs;
        public List<DatLoader.Entity.Stab> VisibleCells;
        public bool SeenOutside;
        public List<uint> VoyeurTable;
        public Landblock CurLandblock;

        public ObjCell(): base()
        {
            Init();
        }

        public ObjCell(uint cellID): base()
        {
            ID = cellID;
            Init();
        }

        public void AddObject(PhysicsObj obj)
        {
            // check for existing obj?
            ObjectList.Add(obj);
            NumObjects++;
            if (obj.ID == 0 || obj.Parent != null || obj.State.HasFlag(PhysicsState.Hidden) || VoyeurTable == null)
                return;

            foreach (var voyeur_id in VoyeurTable)
            {
                if (voyeur_id != obj.ID && voyeur_id != 0)
                {
                    var voyeur = obj.GetObjectA(voyeur_id);
                    if (voyeur == null) continue;

                    var info = new DetectionInfo(obj.ID, DetectionType.EnteredDetection);
                    voyeur.receive_detection_update(info);
                }
            }
        }
        public void AddShadowObject(ShadowObj shadowObj)
        {
            ShadowObjectList.Add(shadowObj);
            NumShadowObjects++;     // can probably replace with .Count
            shadowObj.Cell = this;
        }

        public void CheckAttack(uint attackerID, Position attackerPos, float attackerScale, AttackCone attackCone, AttackInfo attackInfo)
        {
            foreach (var shadowObj in ShadowObjectList)
            {
                var pObj = shadowObj.PhysicsObj;
                if (pObj.ID == attackerID || pObj.State.HasFlag(PhysicsState.Static)) continue;

                var hitLocation = pObj.check_attack(attackerPos, attackerScale, attackCone, attackInfo.AttackRadius);
                if (hitLocation != 0)
                    attackInfo.AddObject(pObj.ID, hitLocation);
            }
        }

        public bool Equals(ObjCell objCell)
        {
            if (objCell == null)
                return false;

            return ID.Equals(objCell.ID);
        }

        public virtual TransitionState FindCollisions(Transition transition)
        {
            return TransitionState.Invalid;
        }

        public virtual TransitionState FindEnvCollisions(Transition transition)
        {
            return TransitionState.Invalid;
        }

        public TransitionState FindObjCollisions(Transition transition)
        {
            var path = transition.SpherePath;

            if (path.InsertType == InsertType.InitialPlacement)
                return TransitionState.OK;

            var target = transition.ObjectInfo.Object.ProjectileTarget;

            // TODO: find out what is causing the exception when .ToList() is not used.
            foreach (var shadowObj in ShadowObjectList.ToList())
            {
                var obj = shadowObj.PhysicsObj;

                if (obj.Parent != null || obj.Equals(transition.ObjectInfo.Object))
                    continue;

                // clip through dynamic non-target objects
                if (target != null && !obj.Equals(target) && !obj.State.HasFlag(PhysicsState.Static))
                    continue;

                var state = obj.FindObjCollisions(transition);
                if (state != TransitionState.OK)
                    return state;
            }
            return TransitionState.OK;
        }

        public static ObjCell Get(uint cellID)
        {
            if (cellID == 0) return null;

            var objCell = new ObjCell(cellID);
            if (cellID >= 0x100)
                return (EnvCell)DBObj.Get(new QualifiedDataID(3, cellID));
            else
                return LandCell.Get(cellID);
        }

        public PhysicsObj GetObject(int id)
        {
            foreach (var obj in ObjectList)
            {
                if (obj != null && obj.ID == id)
                    return obj;
            }
            return null;
        }

        public static ObjCell GetVisible(uint cellID)
        {
            if (cellID == 0) return null;

            // is this supposed to return a list?
            /*if ((cellID & 0xFFFF) >= 0x100)
               return EnvCell.get_visible(cellID);
            else
                return LandCell.Get(cellID);*/
            return LScape.get_landcell(cellID);
        }

        public void Init()
        {
            Pos = new Position();
            ObjectList = new List<PhysicsObj>();
            ShadowObjectList = new List<ShadowObj>();
            VoyeurTable = new List<uint>();
        }

        public void RemoveObject(PhysicsObj obj)
        {
            ObjectList.Remove(obj);
            NumObjects--;
            update_all_voyeur(obj, DetectionType.LeftDetection);
        }

        public bool check_collisions(PhysicsObj obj)
        {
            foreach (var shadowObj in ShadowObjectList)
            {
                var pObj = shadowObj.PhysicsObj;
                if (pObj.Parent == null && !pObj.Equals(obj) && pObj.check_collision(obj))
                    return true;
            }
            return false;
        }

        public TransitionState check_entry_restrictions(Transition transition)
        {
            var objInfo = transition.ObjectInfo;

            if (objInfo.Object == null) return TransitionState.Collided;
            if (objInfo.Object.WeenieObj == null) return TransitionState.OK;

            // check against world object
            return TransitionState.OK;
        }

        public static void find_cell_list(Position position, int numSphere, List<Sphere> sphere, CellArray cellArray, ref ObjCell currCell, SpherePath path)
        {
            cellArray.NumCells = 0;
            cellArray.AddedOutside = false;

            var visibleCell = GetVisible(position.ObjCellID);

            if ((position.ObjCellID & 0xFFFF) >= 0x100)
            {
                if (path != null)
                    path.HitsInteriorCell = true;

                cellArray.add_cell(position.ObjCellID, visibleCell);
            }
            else
                LandCell.add_all_outside_cells(position, numSphere, sphere, cellArray);

            if (visibleCell != null && numSphere != 0)
            {
                for (var i = 0; i < cellArray.Cells.Count; i++)
                {
                    var cell = cellArray.Cells.Values.ElementAt(i);
                    if (cell == null) continue;

                    cell.find_transit_cells(position, numSphere, sphere, cellArray, path);
                }
                //var checkCells = cellArray.Cells.Values.ToList();
                //foreach (var cell in checkCells)
                    //cell.find_transit_cells(position, numSphere, sphere, cellArray, path);

                if (currCell != null)
                {
                    currCell = null;
                    foreach (var cell in cellArray.Cells.Values)
                    {
                        if (cell == null) continue;

                        var blockOffset = LandDefs.GetBlockOffset(position.ObjCellID, cell.ID);
                        var localPoint = sphere[0].Center - blockOffset;

                        if (cell.point_in_cell(localPoint))
                        {
                            currCell = cell;
                            if ((cell.ID & 0xFFFF) >= 0x100)
                            {
                                if (path != null) path.HitsInteriorCell = true;
                                return;     // break?
                            }
                        }
                    }
                }
            }
            if (!cellArray.LoadCells && (position.ObjCellID & 0xFFFF) >= 0x100)
            {
                var cells = cellArray.Cells.Values.ToList();
                foreach (var cell in cells)
                {
                    if (cell == null) continue;

                    if (visibleCell.ID == cell.ID)
                        continue;

                    var found = false;

                    foreach (var stab in ((EnvCell)visibleCell).VisibleCells.Values)
                    {
                        if (cell.ID == stab.ID)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        cellArray.remove_cell(cell);
                }
            }
        }

        public static void find_cell_list(Position position, int numCylSphere, List<CylSphere> cylSphere, CellArray cellArray, SpherePath path)
        {
            if (numCylSphere > 10)
                numCylSphere = 10;

            var spheres = new List<Sphere>();

            for (var i = 0; i < numCylSphere; i++)
            {
                var sphere = new Sphere();
                sphere.Center = position.LocalToGlobal(cylSphere[i].LowPoint);
                sphere.Radius = cylSphere[i].Radius;
                spheres.Add(sphere);
            }

            ObjCell empty = null;
            find_cell_list(position, numCylSphere, spheres, cellArray, ref empty, path);
        }

        public static void find_cell_list(Position position, Sphere sphere, CellArray cellArray, SpherePath path)
        {
            var globalSphere = new Sphere();
            globalSphere.Center = position.LocalToGlobal(sphere.Center);
            globalSphere.Radius = sphere.Radius;

            ObjCell empty = null;
            find_cell_list(position, 1, globalSphere, cellArray, ref empty, path);
        }

        public static void find_cell_list(CellArray cellArray, ref ObjCell checkCell, SpherePath path)
        {
            find_cell_list(path.CheckPos, path.NumSphere, path.GlobalSphere, cellArray, ref checkCell, path);
        }

        public static void find_cell_list(Position position, int numSphere, Sphere sphere, CellArray cellArray, ref ObjCell currCell, SpherePath path)
        {
            find_cell_list(position, numSphere, new List<Sphere>() { sphere }, cellArray, ref currCell, path);
        }

        public virtual void find_transit_cells(int numParts, List<PhysicsPart> parts, CellArray cellArray)
        {
            // empty base
        }

        public virtual void find_transit_cells(Position position, int numSphere, List<Sphere> sphere, CellArray cellArray, SpherePath path)
        {
            // empty base
        }

        public LandDefs.WaterType get_block_water_type()
        {
            if (CurLandblock != null)
                return CurLandblock.WaterType;
            else
                return LandDefs.WaterType.NotWater;
        }

        public float get_water_depth(Vector3 point)
        {
            if (WaterType == LandDefs.WaterType.NotWater)
                return 0.0f;

            if (WaterType == LandDefs.WaterType.EntirelyWater)
                return 0.89999998f;

            if (CurLandblock != null)
                return CurLandblock.calc_water_depth(ID, point);
            else
                return 0.1f;
        }

        public void hide_object(PhysicsObj obj)
        {
            update_all_voyeur(obj, DetectionType.LeftDetection);
        }

        public void init_objects()
        {
            foreach (var obj in ObjectList)
                if (!obj.State.HasFlag(PhysicsState.Static) && !obj.is_completely_visible())
                    obj.recalc_cross_cells();
        }

        public virtual bool point_in_cell(Vector3 point)
        {
            return false;
        }

        public void release_objects()
        {
            while (NumShadowObjects > 0)
            {
                var shadowObj = ShadowObjectList[0];
                remove_shadow_object(shadowObj);

                shadowObj.PhysicsObj.remove_parts(this);
            }
            //if (NumObjects > 0 && ObjMaint != null)
                //ObjMaint.ReleaseObjCell(this);
        }

        public void remove_shadow_object(ShadowObj shadowObj)
        {
            // multiple shadows?
            ShadowObjectList.Remove(shadowObj);
            shadowObj.Cell = null;
            NumShadowObjects--;
        }

        public void unhide_object(PhysicsObj obj)
        {
            update_all_voyeur(obj, DetectionType.EnteredDetection, false);
        }

        public void update_all_voyeur(PhysicsObj obj, DetectionType type, bool checkDetection = true)
        {
            if (obj.ID == 0 || obj.Parent != null || VoyeurTable == null)
                return;

            if (obj.State.HasFlag(PhysicsState.Hidden) && (checkDetection ? type == DetectionType.EnteredDetection : true))
                return;

            foreach (var voyeur_id in VoyeurTable)
            {
                if (voyeur_id != obj.ID && voyeur_id != 0)
                {
                    var voyeur = obj.GetObjectA(voyeur_id);
                    if (voyeur == null) continue;

                    var info = new DetectionInfo(obj.ID, type);
                    voyeur.receive_detection_update(info);
                }
            }
        }
    }
}
