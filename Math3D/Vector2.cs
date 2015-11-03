﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Binarysharp.MemoryManagement.Math3D
{
    /// <summary>
    ///     Class that holds information about a 2d-coordinate and offers some basic operations.
    /// </summary>
    public struct Vector2
    {
        #region VARIABLES

        public float X;
        public float Y;

        #endregion VARIABLES

        #region PROPERTIES

        /// <summary>
        ///     Returns a new Vector3 at (1,0)
        /// </summary>
        public static Vector2 UnitX => new Vector2(1, 0);

        /// <summary>
        ///     Returns a new Vector2 at (0,1)
        /// </summary>
        public static Vector2 UnitY => new Vector2(0, 1);

        /// <summary>
        ///     Returns a new Vector2 at (0,0)
        /// </summary>
        public static Vector2 Zero => new Vector2(0, 0);

        #endregion PROPERTIES

        #region CONSTRUCTOR

        /// <summary>
        ///     Initializes a new Vector2 using the given values
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        ///     Initializes a new Vector2 by copying the values of the given Vector2
        /// </summary>
        /// <param name="vec"></param>
        public Vector2(Vector2 vec) : this(vec.X, vec.Y)
        {
        }

        /// <summary>
        ///     Initializes a new Vector2 using the given float-array
        /// </summary>
        /// <param name="values"></param>
        public Vector2(IReadOnlyList<float> values) : this(values[0], values[1])
        {
        }

        #endregion CONSTRUCTOR

        #region METHODS

        /// <summary>
        ///     Returns the distance from this Vector2 to the given Vector2
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public float DistanceTo(Vector2 other) => (this + other).Length();

        public override bool Equals(object obj)
        {
            var vec = (Vector2) obj;
            return GetHashCode() == vec.GetHashCode();
        }

        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode();

        /// <summary>
        ///     Returns the length of this Vector2
        /// </summary>
        /// <returns></returns>
        public float Length() => (float) Math.Sqrt(Math.Pow(X, 2) + Math.Pow(Y, 2));

        public override string ToString()
            => $"[X={X.ToString(CultureInfo.InvariantCulture)}, Y={Y.ToString(CultureInfo.InvariantCulture)}]";

        #endregion METHODS

        #region OPERATORS

        public float this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0:
                        return X;

                    case 1:
                        return Y;

                    default:
                        throw new IndexOutOfRangeException();
                }
            }
            set
            {
                switch (i)
                {
                    case 0:
                        X = value;
                        break;

                    case 1:
                        Y = value;
                        break;

                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }

        public static Vector2 operator -(Vector2 v1, Vector2 v2) => new Vector2(v1.X - v2.X, v1.Y - v2.Y);
        public static bool operator !=(Vector2 v1, Vector2 v2) => !(v1 == v2);
        public static Vector2 operator *(Vector2 v1, float scalar) => new Vector2(v1.X*scalar, v1.Y*scalar);
        public static Vector2 operator +(Vector2 v1, Vector2 v2) => new Vector2(v1.X + v2.X, v1.Y + v2.Y);

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public static bool operator ==(Vector2 v1, Vector2 v2) => v1.X == v2.X && v1.Y == v2.Y;

        #endregion OPERATORS
    }
}