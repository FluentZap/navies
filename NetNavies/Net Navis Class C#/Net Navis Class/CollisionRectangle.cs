using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CollisionClasses
{
    [Serializable]
    public class CollisionRectangle
    {
        protected Vector2 center;
        protected Vector2 vertexOffset;
        protected float rotation;
        protected Vector2[] vertices = new Vector2[4];
        protected Vector2[] axes = new Vector2[2];
        protected bool verticesDirty = true;
        protected bool axesDirty = true;

        #region Properties
        public Vector2 Center
        {
            get { return center; }
            set
            {
                center = value;
                verticesDirty = true;
            }
        }
        public Vector2 VertexOffset
        {
            get { return vertexOffset; }
            set
            {
                vertexOffset = value;
                verticesDirty = true;
                axesDirty = true;
            }
        }
        public float Rotation
        {
            get { return rotation; }
            set
            {
                rotation = value;
                verticesDirty = true;
                axesDirty = true;
            }
        }
        public Vector2[] Vertices
        {
            get
            {
                if (verticesDirty)
                    RecalculateVertices();
                return vertices;
            }
        }
        public Vector2[] Axes
        {
            get
            {
                if (axesDirty)
                    RecalculateAxes();
                return axes;
            }
        }
        public bool VerticesDirty
        {
            get { return verticesDirty; }
        }
        public bool AxesDirty
        {
            get { return axesDirty; }
        }
        #endregion

        // Constructor
        public CollisionRectangle(Vector2 center, Vector2 vertexOffset, float rotation)
        {
            this.center = center;
            this.vertexOffset = vertexOffset;
            this.rotation = rotation;
        }

        public void SetCenterX(float x)
        {
            center.X = x;
            verticesDirty = true;
        }

        public void SetCenterY(float y)
        {
            center.Y = y;
            verticesDirty = true;
        }

        /// <summary>
        /// If the properties of the rectangle change (angle, position, bounds), this will be
        /// called automatically the next time the vertices are accessed. You can call it
        /// manually for added control over performance.
        /// </summary>
        public void RecalculateVertices()
        {
            float cosX = (float)Math.Cos(rotation) * vertexOffset.X;
            float cosY = (float)Math.Cos(rotation) * vertexOffset.Y;
            float sinX = (float)Math.Sin(rotation) * vertexOffset.X;
            float sinY = (float)Math.Sin(rotation) * vertexOffset.Y;

            vertices[0] = new Vector2(cosX - sinY + center.X, sinX + cosY + center.Y);
            vertices[1] = new Vector2(cosX + sinY + center.X, sinX - cosY + center.Y);
            vertices[2] = new Vector2(-cosX + sinY + center.X, -sinX - cosY + center.Y);
            vertices[3] = new Vector2(-cosX - sinY + center.X, -sinX + cosY + center.Y);
            verticesDirty = false;
        }

        /// <summary>
        /// If the angle or bounds change (not position), this will be
        /// called automatically the next time the vertices are accessed. You can call it
        /// manually for added control over performance.
        /// </summary>
        public void RecalculateAxes()
        {
            if (verticesDirty)
                RecalculateVertices();
            axes[0] = vertices[1] - vertices[2];
            axes[1] = vertices[0] - vertices[1];
            axesDirty = false;
        }

        /// <summary>
        /// Normalizes the axes, recalculating them only if necessary.
        /// </summary>
        public void NormalizeAxes()
        {
            if (axesDirty)
                RecalculateAxes();
            axes[0].Normalize();
            axes[1].Normalize();
        }

        /// <summary>
        /// Performs the dot product on a given vertex, returning that vertex's position on the axis.
        /// </summary>
        /// <param name="vertex">The point to project</param>
        /// <param name="axis">A point on the axis. Does not have to be normalized</param>
        /// <returns></returns>
        public static double DotProduct(Vector2 vertex, Vector2 axis)
        {
            return vertex.X * axis.X + vertex.Y * axis.Y;
        }

        /// <summary>
        /// Projects a shape onto the given axis. Returns a Vector2 representing the min/max points
        /// of the shape's shadow (min=x, max=y).
        /// </summary>
        /// <param name="rect">The shape to project</param>
        /// <param name="axis">A point on the axis. Does not have to be normalized.</param>
        /// <returns></returns>
        public Vector2 Project(Vector2 axis)
        {
            double min, max, dotProduct;
            if (verticesDirty)
                RecalculateVertices();
            min = max = vertices[0].X * axis.X + vertices[0].Y * axis.Y; // Get dot product. In-lining to allow compiler optimization
            for (int i = 1; i < 4; ++i)
            {
                dotProduct = vertices[i].X * axis.X + vertices[i].Y * axis.Y;
                if (dotProduct < min)
                    min = dotProduct;
                else if (dotProduct > max)
                    max = dotProduct;
            }
            return new Vector2((float)min, (float)max);
        }

        /// <summary>
        /// Given two Vector2s representing min/max on an axis, returns true if they overlap.
        /// </summary>
        /// <param name="a">A Vector2 representing min/max. (min=x, max=y).</param>
        /// <param name="b">A Vector2 representing min/max. (min=x, max=y).</param>
        /// <returns></returns>
        public static bool CheckOverlap(Vector2 a, Vector2 b)
        {
            if (a.Y < b.X || b.Y < a.X)
                return false;
            return true;
        }

        /// <summary>
        /// Given two Vector2s representing min/max on an axis, returns the minimum amount 
        /// that A needs in order to be pushed in order to separate.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static float FindOverlap(Vector2 a, Vector2 b)
        {
            float overlap1 = a.Y - b.X;
            float overlap2 = b.Y - a.X;
            if (overlap1 < overlap2)
                return -overlap1;
            else
                return overlap2;
        }

        /// <summary>
        /// Returns true if this shape intersects with the given shape.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Intersects(CollisionRectangle other)
        {
            Vector2 projectionA, projectionB;
            foreach (Vector2 axis in this.Axes)
            {
                projectionA = Project(axis);
                projectionB = other.Project(axis);
                // check for overlap. In-lining this function to allow compiler optimization
                if (projectionA.Y < projectionB.X || projectionB.Y < projectionA.X)
                    return false;
            }
            foreach (Vector2 axis in other.Axes)
            {
                projectionA = Project(axis);
                projectionB = other.Project(axis);
                if (projectionA.Y < projectionB.X || projectionB.Y < projectionA.X)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Returns true if this shape intersects with the given shape. Also returns the
        /// minimum translation vector, or MTV, used to push the shapes apart.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="mtvAxis"></param>
        /// <param name="mtvMagnitude"></param>
        /// <returns></returns>
        public bool IntersectsMTV(CollisionRectangle other, out Vector2 mtvAxis, out float mtvMagnitude)
        {
            mtvAxis = Vector2.Zero;
            mtvMagnitude = 4000000000; // really large value
            float overlap;
            Vector2 projectionA, projectionB;
            this.NormalizeAxes();
            other.NormalizeAxes();
            foreach (Vector2 axis in this.Axes)
            {
                projectionA = Project(axis);
                projectionB = other.Project(axis);
                // check for overlap. In-lining this function to allow compiler optimization
                if (projectionA.Y < projectionB.X || projectionB.Y < projectionA.X)
                    return false;
                else
                {
                    overlap = FindOverlap(projectionA, projectionB);
                    if (Math.Abs(overlap) < Math.Abs(mtvMagnitude))
                    {
                        mtvMagnitude = overlap;
                        mtvAxis = axis;
                    }
                }
            }
            foreach (Vector2 axis in other.Axes)
            {
                projectionA = Project(axis);
                projectionB = other.Project(axis);
                if (projectionA.Y < projectionB.X || projectionB.Y < projectionA.X)
                    return false;
                else
                {
                    overlap = FindOverlap(projectionA, projectionB);
                    if (Math.Abs(overlap) < Math.Abs(mtvMagnitude))
                    {
                        mtvMagnitude = overlap;
                        mtvAxis = axis;
                    }
                }
            }
            return true;
        }

        public bool FindTranslationMagnitude(CollisionRectangle other, Vector2 translationAxis, out float magnitude)
        {
            float tempMagnitude;
            Vector2 projectionA, projectionB;
            magnitude = 4000000000; // really large value;

            this.NormalizeAxes();
            other.NormalizeAxes();
            if (translationAxis == Vector2.Zero)
                return false;
            translationAxis.Normalize();

            HashSet<Vector2> possibleAxes = new HashSet<Vector2>();
            double distance;
            double angle;
            // Collect the axis that form an acute angle with the axis of pushback
            foreach (Vector2 axis in this.Axes)
            {
                distance = Math.Pow((translationAxis.X - axis.X), 2) + Math.Pow((translationAxis.Y - axis.Y), 2);
                angle = Math.Acos(1 - distance / 2);
                if (angle < Math.PI / 2)
                    possibleAxes.Add(axis);
                else
                    possibleAxes.Add(axis * -1);
            }
            foreach (Vector2 axis in other.Axes)
            {
                distance = Math.Pow((translationAxis.X - axis.X), 2) + Math.Pow((translationAxis.Y - axis.Y), 2);
                angle = Math.Acos(1 - distance / 2);
                if (angle < Math.PI / 2)
                    possibleAxes.Add(axis);
                else
                    possibleAxes.Add(axis * -1);
            }

            // Calculate the shapes' for each axis. Save the minimum overlap and the axis.
            foreach (Vector2 axis in possibleAxes)
            {
                projectionA = Project(axis);
                projectionB = other.Project(axis);
                // check for overlap. In-lining this function to allow compiler optimization
                if (projectionA.Y < projectionB.X || projectionB.Y < projectionA.X)
                    return false;
                else
                {
                    float overlap = projectionB.Y - projectionA.X;
                    float dotProd = (float)DotProduct(axis, translationAxis);
                    tempMagnitude = overlap / dotProd;
                    if (Math.Abs(tempMagnitude) < Math.Abs(magnitude))
                        magnitude = tempMagnitude;
                }
            }

            //magnitude = mtvMagnitude / (float)DotProduct(mtvAxis, translationAxis);
            return true;
        }
    }
}
