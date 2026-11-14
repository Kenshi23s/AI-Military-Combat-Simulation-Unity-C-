
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FacundoColomboMethods
{
    /// <summary>Cast shape for GetWhichAreOnSight.</summary>
    public enum RaycastType
    {
        Sphere,
        Default
    }

    /// <summary>Line of sight requirement.</summary>
    public enum MustBeOnSight
    {
        Yes,
        No
    }

    /// <summary>General helpers for vectors, raycasts, names, collections and gizmos.</summary>
    public static class ColomboMethods
    {
        /// <summary>Frame rate of the last frame.</summary>
        public static int GetActualFrameRate => (int)(1f/Time.deltaTime);


        /// <summary>Draws a wire cylinder gizmo of height and radius at position.</summary>
        /// <param name="drawFromBase">true places position at the base. false places it at the center.</param>
        public static void DrawCylinder(this Vector3 position, Quaternion orientation, float height, float radius, bool drawFromBase = true)
        {
            Vector3 localUp = orientation * Vector3.up;
            Vector3 localRight = orientation * Vector3.right;
            Vector3 localForward = orientation * Vector3.forward;

            Vector3 basePositionOffset = drawFromBase ? Vector3.zero : (localUp * height * 0.5f);
            Vector3 basePosition = position - basePositionOffset;
            Vector3 topPosition = basePosition + localUp * height;

            Quaternion circleOrientation = orientation * Quaternion.Euler(90, 0, 0);

            Vector3 pointA = basePosition + localRight * radius;
            Vector3 pointB = basePosition + localForward * radius;
            Vector3 pointC = basePosition - localRight * radius;
            Vector3 pointD = basePosition - localForward * radius;

            Gizmos.DrawRay(pointA, localUp * height);
            Gizmos.DrawRay(pointB, localUp * height);
            Gizmos.DrawRay(pointC, localUp * height);
            Gizmos.DrawRay(pointD, localUp * height);

            DrawCircle(basePosition, circleOrientation, radius, 32);
            DrawCircle(topPosition, circleOrientation, radius, 32);
        }

        /// <summary>Draws a wire circle gizmo of radius with segments lines at position.</summary>
        public static void DrawCircle(this Vector3 position, Quaternion rotation, float radius, int segments)
        {
            // If either radius or number of segments are less or equal to 0, skip drawing
            if (radius <= 0.0f || segments <= 0)
            {
                return;
            }

            // Single segment of the circle covers (360 / number of segments) degrees
            float angleStep = (360.0f / segments);

            // Result is multiplied by Mathf.Deg2Rad constant which transforms degrees to radians
            // which are required by Unity's Mathf class trigonometry methods

            angleStep *= Mathf.Deg2Rad;

            // lineStart and lineEnd variables are declared outside of the following for loop
            Vector3 lineStart = Vector3.zero;
            Vector3 lineEnd = Vector3.zero;

            for (int i = 0; i < segments; i++)
            {
                // Line start is defined as starting angle of the current segment (i)
                lineStart.x = Mathf.Cos(angleStep * i);
                lineStart.y = Mathf.Sin(angleStep * i);
                lineStart.z = 0.0f;

                // Line end is defined by the angle of the next segment (i+1)
                lineEnd.x = Mathf.Cos(angleStep * (i + 1));
                lineEnd.y = Mathf.Sin(angleStep * (i + 1));
                lineEnd.z = 0.0f;

                // Results are multiplied so they match the desired radius
                lineStart *= radius;
                lineEnd *= radius;

                // Results are multiplied by the rotation quaternion to rotate them 
                // since this operation is not commutative, result needs to be
                // reassigned, instead of using multiplication assignment operator (*=)
                lineStart = rotation * lineStart;
                lineEnd = rotation * lineEnd;

                // Results are offset by the desired position/origin 
                lineStart += position;
                lineEnd += position;

                // Points are connected using DrawLine method and using the passed color
                Gizmos.DrawLine(lineStart, lineEnd);
            }
        }

        /// <summary>Returns a Vector3 with value on the three axes.</summary>
        public static Vector3 ToVector(this float value)
        {
            return new Vector3(value, value, value);
        }
        /// <summary>Returns a random element of col. Every element has the same chance.</summary>
        /// <param name="col">Collection with at least one element.</param>
        /// <remarks>O(1) for an IList. O(n) for any other collection.</remarks>
        public static T PickRandom<T>(this IEnumerable<T> col)
        {
            IList<T> list = col as IList<T> ?? col.ToList();
            return list[Random.Range(0, list.Count)];
        }

        /// <summary>Returns color with newAlpha as its alpha.</summary>
        public static Color SetAlpha(this Color color,float newAlpha)
        {
             return new Color(color.r, color.g, color.b, newAlpha);
        }

        /// <summary>Returns the hit point of a raycast from myPos along dir, or myPos when the ray hits nothing.</summary>
        public static Vector3 TryGetMeshCollision(this Vector3 myPos,Vector3 dir,LayerMask layer)
        {
            if (Physics.Raycast(myPos,dir,out RaycastHit hit,Mathf.Infinity,layer))            
                return hit.point;           
            else
                return myPos;
        }

        /// <summary>Returns the angle and the normal of the floor below myPos. Returns 0 and Vector3.zero without floor.</summary>
        public static Tuple<float,Vector3> GetNormalAngleOfFloor(this Vector3 myPos,LayerMask layer)
        {
            if (Physics.Raycast(myPos, Vector3.down, out RaycastHit hit, Mathf.Infinity, layer))
            {
                return Tuple.Create<float, Vector3>(Vector3.Angle(hit.normal, Vector3.up),hit.normal); 
            }
            return Tuple.Create<float, Vector3>(0,Vector3.zero);
        }

        /// <summary>Returns V expressed in the local axes of tr.</summary>
        public static Vector3 GetOrientedVector(this Transform tr, Vector3 V)
        {
            return V.x * tr.right + V.y * tr.up + tr.forward * V.z;
        }

        /// <summary>Returns V expressed in the local axes of tr.</summary>
        public static Vector3 GetOrientedVector(this Vector3 V,Transform tr)
        {
            return V.x * tr.right + V.y * tr.up + tr.forward * V.z;
        }


        /// <summary>Returns a random name of alternating consonants and vowels with at least len letters.</summary>
        public static string GenerateName(int len)
        {
            System.Random r = new System.Random();
            string[] consonants = { "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "l", "n", "p", "q", "r", "s", "sh", "zh", "t", "v", "w", "x" };
            string[] vowels = { "a", "e", "i", "o", "u", "ae", "y" };
            string Name = "";
            Name += consonants[r.Next(consonants.Length)].ToUpper();
            Name += vowels[r.Next(vowels.Length)];
            int b = 2; //b tells how many times a new letter has been added. It's 2 right now because the first two letters are already in the name.
            while (b < len)
            {
                Name += consonants[r.Next(consonants.Length)];
                b++;
                Name += vowels[r.Next(vowels.Length)];
                b++;
            }

            return Name;


        }

        /// <summary>Returns quantity names from GenerateName with wordLenght letters.</summary>
        public static IEnumerable<string> GenerateNames(this int quantity,int wordLenght)
        {
            var names = new List<string>();
            for (int i = 0; i < quantity; i++)
            {
                names.Add(GenerateName(wordLenght));
            }

            return names;


        }

        /// <summary>Returns the layer index of a mask with a single layer.</summary>
        public static int LayerMaskToLayerNumber(this LayerMask x) => Mathf.RoundToInt(Mathf.Log(x, 2));

        /// <summary>Adds item when col does not contain it.</summary>
        public static void CheckAndAdd<T>(this List<T> col,T item)
        {
            if (!col.Contains(item))
            {
                col.Add(item);
            }
        }

        /// <summary>Removes item from col.</summary>
        public static void CheckAndRemove<T>(this List<T> col, T item)
        {
            if (col.Contains(item)) col.Remove(item);
        }

        /// <summary>Returns true when value is greater than moreThan and less than lessThan.</summary>
        public static bool InBetween(this float value, float lessThan, float moreThan) => value < lessThan && value > moreThan;

        /// <summary>Returns 1 at target and 0 at radius or farther, linear in between.</summary>
        public static float InverseDistanceScalar(this Vector3 pos,Vector3 target,float radius)
        {
            // la distancia / el radio
            // pongo 1 - (valor entre 0 y 1) porque:
            // si esta en el limite de la distancia, la division me daria 1, por lo que recibiria la maxima fuerza si no lo restara por -1
            // y por si estoy en el centro, la division seria " 0 / 1" y en caso de que no le restara -1, no recibiria la maxima fuerza
            // deberia informarme mas de matematica para videojuegos, sin jocha no podria haber sacado esta "Magnitud Inversa"
            return Mathf.Max(1 - (Vector3.Distance(pos, target) / radius),0f);           
        }

        /// <summary>Returns the T components of the colliders within radius of pos.</summary>
        public static IEnumerable<T> GetItemsOFTypeAround<T>(this Vector3 pos, float radius)
        {
            return Physics.OverlapSphere(pos, radius)
                 .Where(x => x.TryGetComponent(out T arg))
                 .Select(x => x.GetComponent<T>());

        }

        /// <summary>Returns myDir rotated toward a random direction by up to angle degrees.</summary>
        /// <param name="angle">Spread in degrees, from 0 to 180.</param>
        public static Vector3 RandomDirFrom(this Vector3 myDir,float angle)
        {
            // el angulo maximo entre 2 vectores es 180, si se pasa vuelvea a empezar de 0
            Vector3 random = Random.insideUnitSphere;

            angle =  Mathf.Clamp(MathF.Abs(angle),0,180);
            // lo divido por 180 para "Remapearlo a un valor mas bajo"     
            return Vector3.Slerp(myDir.normalized, random, angle / 180f); ;
        }

        /// <summary>Returns a shallow copy of listToClone.</summary>
        public static List<T> CloneList<T>(List<T> listToClone)
        {
            List<T> aux = new List<T>();
            for (int i = 0; i < listToClone.Count; i++)
            {
                aux.Add(listToClone[i]);
            }
            return aux;
        }
        /// <summary>Returns the T component of each direct child of Father, by child index.</summary>
        /// <returns>An array of Father.childCount with default for the children without T.</returns>
        public static T[] GetChildrenComponents<T>(this Transform Father)
        {
            T[] Components = new T[Father.childCount];
            for (int i = 0; i < Father.childCount; i++)
            {
                var item = Father.transform.GetChild(i).GetComponent<T>();
                if (item != null)
                {
                    Components[i] = item;
                }
            }
            return Components;
        }
        /// <summary>Returns the T components of the colliders within radius of tr.</summary>
        public static List<T> GetNearby<T>(this Transform tr, float radius)
        {
            List<T> list = new List<T>();
            Collider[] colliders = Physics.OverlapSphere(tr.position, radius);

            foreach (Collider Object in colliders)
            {
                T item = Object.GetComponent<T>();
                if (item != null)
                {
                    list.Add(item);
                }
            }


            return list;
        }
        /// <summary>Returns true when a T within radius of pos is in line of sight through Wall.</summary>
        public static bool CheckNearbyInSigth<T>(this Transform pos, float radius, LayerMask Wall) where T : MonoBehaviour
        {
            
            Collider[] colliders = Physics.OverlapSphere(pos.position, radius);
            foreach (Collider Object in colliders)
            {
                var item = Object.GetComponent<T>();
                if (item != null)
                {
                    if (InLineOffSight(pos.position, item.transform.position, Wall))
                    {
                        return true;
                    }

                }
            }

            return false;
        }

        /// <summary>Returns the T components within radius of pos that are in line of sight through Wall.</summary>
        public static List<T> GetALLNearbyInSigth<T>(this Transform pos, float radius,LayerMask Wall) where T: MonoBehaviour
        {
            List<T> list = new List<T>();
            Collider[] colliders = Physics.OverlapSphere(pos.position, radius);

            foreach (Collider Object in colliders)
            {
                var item = Object.GetComponent<T>();
                if (item != null)
                {
                    if (InLineOffSight(pos.position,item.transform.position, Wall))
                    {
                        list.Add(item);
                    }
                  
                }
            }

            return list;
        }

        /// <summary>Returns true when the segment from start to end hits nothing in maskWall.</summary>
        public static bool InLineOffSight(this Vector3 start, Vector3 end, LayerMask maskWall)
        {
            Vector3 dir = end - start;

            return !Physics.Raycast(start, dir, dir.magnitude, maskWall);
        }

        /// <summary>
        /// Returns true when end is within maxDistance of start and the segment between them hits nothing in maskWall.
        /// </summary>
        /// <remarks>The raycast covers the segment from start to end.</remarks>
        public static bool InLineOffSight(this Vector3 start, Vector3 end, LayerMask maskWall, float maxDistance)
        {
            Vector3 dir = end - start;
            float sqrDistance = dir.sqrMagnitude;
            if (sqrDistance > maxDistance * maxDistance)
                return false;

            return !Physics.Raycast(start, dir, Mathf.Sqrt(sqrDistance), maskWall);
        }

        /// <summary>Returns the hit point of a raycast from pos along dir, or pos moved range units on Z.</summary>
        public static Vector3 CheckForwardRayColision(Vector3 pos, Vector3 dir, float range = 15)
        {
            //aca se guardan los datos de con lo que impacte el rayo
            RaycastHit hit;

            //si el rayo choco contra algo
            if (Physics.Raycast(pos, dir, out hit, range))
            {
                return hit.point;

            }

            return new Vector3(pos.x, pos.y, pos.z + range);

        }
        #region Line of sight

        /// <summary>Returns the nearest item of objPosition with line of sight from myPos, or null.</summary>
        public static T GetNearestOnSight<T>(this Vector3 myPos, List<T> objPosition,LayerMask walls) where T : MonoBehaviour
        {
            List<T> listOnSight = GetWhichAreOnSight(objPosition, myPos,walls);

            switch (listOnSight.Count)
            {
                  
                //ninguno a la vista
                case 0:
                    return null;
                //1 a la vista
                case 1:
                    return listOnSight[0];
                //mas de  1 a la vista
                default:
                 float nearest = (listOnSight[0].transform.position - myPos).sqrMagnitude;
                 int nearestIndex = 0;

                 for (int i = 1; i < listOnSight.Count; i++)
                 {
                     float tempMagnitude = (listOnSight[i].transform.position - myPos).sqrMagnitude;
                   
                     if (nearest > tempMagnitude)
                     {
                         nearest = tempMagnitude;
                         nearestIndex = i;
                     }
                 }
                  
                 return listOnSight[nearestIndex];

                    
            }                          
        }

        /// <summary>Returns the items with line of sight from pos.</summary>
        /// <param name="type">Sphere casts a sphere of radius against wallMask. Default casts a ray against every layer.</param>
        public static List<T> GetWhichAreOnSight<T>(this List<T> itemsPassed, Vector3 pos,  LayerMask wallMask = default, RaycastType type = RaycastType.Default, float radius=10f) where T : MonoBehaviour
        {
            switch (type)
            {
                case RaycastType.Sphere:
                    return FacundoSphereCastAll(pos, itemsPassed,radius, wallMask);                 
                    
                default:
                  return FacundoRaycastAll(pos, itemsPassed);
                    
            }
          
        }
        /// <summary>Returns item when it has line of sight from pos, or default.</summary>
        public static T Get_IsOnSight<T>(T item, Vector3 pos, LayerMask layer, RaycastType type = RaycastType.Default, float sphereRadius = 0) where T : MonoBehaviour
        {

            Vector3 dir = item.transform.position - pos;
            switch (type)
            {
                case RaycastType.Sphere:
                    if (FacundoSphereCast(pos, dir,sphereRadius,layer))
                    {
                        return item;
                    }
                    break;
                default:
                    if (FacundoRaycast<T>(pos, dir, item))
                    {
                        return item;
                    }

                    break;
            }
            return (T)default;

        }

        // mis raycasts
        private static bool FacundoRaycast<T>(Vector3 pos, Vector3 dir, T item) where T : MonoBehaviour
        {
            RaycastHit hit;
            if (!Physics.Raycast(pos, dir, out hit,dir.magnitude))
            {
                return true;
            }
            else
            {
                Transform HitObject = hit.transform;
                if (HitObject == item.transform)
                {
                    return true;
                }
            }
            return false;


        }

        private static List<T> FacundoRaycastAll<T>(Vector3 pos, List<T> items) where T : MonoBehaviour
        {
            List<T> Tlist = items.Where(x =>
            {
                Vector3 dir = x.transform.position - pos;
                return FacundoRaycast(pos, dir, x);
            }).ToList();

            return Tlist.Count >= 1 ? Tlist: new List<T>();
        


        }

        private static bool FacundoSphereCast(Vector3 pos, Vector3 dir, float radius,LayerMask layer) 
        {
            return !Physics.SphereCast(pos, radius, dir, out RaycastHit hit, dir.magnitude, layer);
        }

        private static List<T> FacundoSphereCastAll<T>(Vector3 pos, List<T> items, float radius,LayerMask wallMask) where T : MonoBehaviour
        {   
            return items.Where(x =>
            {
                Vector3 dir = x.transform.position - pos;
                return FacundoSphereCast(pos, dir, radius, wallMask);
            }).ToList();
        }

    }
    #endregion
}
