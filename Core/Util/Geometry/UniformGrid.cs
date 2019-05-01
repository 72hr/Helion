﻿using System;

namespace Helion.Util.Geometry
{
    /// <summary>
    /// A container for a uniformly distributed grid. This also aligns itself 
    /// to the grid so certain optimizations can be done (ex: power of two 
    /// shifting instead of divisions).
    /// </summary>
    /// <typeparam name="T">The block component for each grid element.
    /// </typeparam>
    public class UniformGridFixed<T>
    {
        private const int Dimension = 128;

        /// <summary>
        /// How many blocks wide the grid is.
        /// </summary>
        public readonly int Width;

        /// <summary>
        /// How many blocks tall the grid is.
        /// </summary>
        public readonly int Height;

        /// <summary>
        /// The bounds of the grid.
        /// </summary>
        public readonly Box2Fixed Bounds;

        private readonly T[] blocks;

        /// <summary>
        /// The origin of the grid.
        /// </summary>
        public Vec2Fixed Origin => Bounds.Min;

        public UniformGridFixed(Box2Fixed bounds)
        {
            Bounds = ToBounds(bounds);

            Vec2Fixed sides = Bounds.Sides();
            Width = sides.X.ToInt() / Dimension;
            Height = sides.Y.ToInt() / Dimension;
            blocks = new T[Width * Height];
        }

        /// <summary>
        /// Gets the block at the coordinates provided.
        /// </summary>
        /// <param name="row">The row, should in [0, Width).</param>
        /// <param name="col">The column, should in [0, Height).</param>
        /// <returns></returns>
        /// <exception cref="System.IndexOutOfRangeException">if the indices 
        /// are out of range.</exception>
        public T this[int row, int col] => blocks[(row * Width) + col];

        private static Box2Fixed ToBounds(Box2Fixed bounds)
        {
            // Note that we are subtracting 1 from the bottom left even after
            // clamping it to the left. The reason for this is because any
            // iteration over the grid with very clean and fast code has a 
            // stupid corner case that causes a diagonal line going from the 
            // bottom right corner of [0, 0] to the top left corner of [0, 0] 
            // to end up going outside the grid to [-1, 0]. This obviously will
            // cause an exception.
            //
            // The solution is to pad the left block by making it go to the 
            // left by one. This way when the edge case happens, it won't be 
            // unsafe anymore. The performance impact is technically a net 
            // positive because instead of writing more branches in a taxing 
            // loop iteration that we want to keep at high speeds, we toss out
            // the branch now for every invocation by doing the following, at 
            // the cost of a very small amount of memory in the grid. This is
            // a great trade-off. If we can get the best of both worlds though
            // one day, we should do that.
            int alignedLeftBlock = (bounds.Min.X.Floor().ToInt() / Dimension) - 1;
            int alignedBottomBlock = bounds.Min.Y.Floor().ToInt() / Dimension;
            int alignedRightBlock = (bounds.Max.X.Floor().ToInt() / Dimension) + 1;
            int alignedTopBlock = (bounds.Max.Y.Floor().ToInt() / Dimension) + 1;

            Vec2Fixed origin = new Vec2Fixed(
                new Fixed(alignedLeftBlock * Dimension), 
                new Fixed(alignedBottomBlock * Dimension)
            );

            Vec2Fixed topRight = new Vec2Fixed(
                new Fixed(alignedRightBlock * Dimension), 
                new Fixed(alignedTopBlock * Dimension)
            );

            return new Box2Fixed(origin, topRight);
        }

        private int IndexFromBlockCoordinate(Vec2I coordinate) => coordinate.X + coordinate.Y * Width;

        public bool Iterate(Seg2FixedBase seg, Func<T, bool> func)
        {
            Assert.Precondition(Bounds.Contains(seg.Start), "Segment start point outside of grid");
            Assert.Precondition(Bounds.Contains(seg.End), "Segment end point outside of grid");

            // This algorithm requires us to be on the unit interval range for 
            // our block coordinates. We also want them to be positive, since 
            // it will allow us to do well defined bit mask optimizations.
            // Note that 'blockUnit' means it's on the unit range for a block.
            // For example, if its value is 1.5, that means it's half way in
            // between blocks 1 and 2.
            // What this means is we are quantizing the Dimension onto a unit
            // grid.
            Vec2Fixed blockUnitStart = (seg.Start - Origin) / Dimension;
            Vec2Fixed blockUnitEnd = (seg.End - Origin) / Dimension;
            Vec2Fixed absDelta = (blockUnitEnd - blockUnitStart).Abs();

            // This is the index into the block array. Because its a 1D array,
            // moving upwards in the grid is the same adding the grid width.
            Vec2I startingBlock = blockUnitStart.ToInt();
            int blockIndex = IndexFromBlockCoordinate(startingBlock);

            // This contains the steps that we will add to the block index. As
            // it is a 1D grid, this means horizontal step will be one of {-1,
            // 0, 1}, but the vertical step will be one of {-Width, 0, Width}.
            // The reason the steps are negative is so that we don't need any
            // extra branching to go left or right, we can just add the value
            // and it'll go in the correct direction based on the sign.
            int horizontalStep = 0;
            int verticalStep = 0;

            // Since we are using a Bresenham-like algorithm, this is the error
            // we accumulate when moving along the grid. It tells us whether we
            // want to step along the X axis or the Y axis next.
            Fixed error;

            // We know that the amount of blocks we visit is the number of 
            // times we cross the X and Y axes (which we calculate later), 
            // plus one for the block we start off in.
            int numBlocks = 1;

            // Look at the X axis. Are we not moving anywhere because we are a
            // vertical line? Or are we going left? Or right?
            if (MathHelper.IsZero(absDelta.X))
            {
                error = Fixed.Max();
            }
            else if (blockUnitEnd.X > blockUnitStart.X)
            {
                horizontalStep = 1;
                numBlocks += blockUnitEnd.X.Floor().ToInt() - startingBlock.X;
                error = (blockUnitStart.X.Floor() + 1 - blockUnitStart.X) * absDelta.Y;
            }
            else
            {
                horizontalStep = -1;
                numBlocks += startingBlock.X - blockUnitEnd.X.Floor().ToInt();
                error = (blockUnitStart.X - blockUnitStart.X.Floor()) * absDelta.Y;
            }

            // Same as the above, but now for the Y axis.
            if (MathHelper.IsZero(absDelta.Y))
            {
                error = Fixed.Lowest();
            }
            else if (blockUnitEnd.Y > blockUnitStart.Y)
            {
                verticalStep = Width;
                numBlocks += blockUnitEnd.Y.Floor().ToInt() - startingBlock.Y;
                error -= (blockUnitStart.Y.Floor() + 1 - blockUnitStart.Y) * absDelta.X;
            }
            else
            {
                horizontalStep = -Width;
                numBlocks += startingBlock.Y - blockUnitEnd.Y.Floor().ToInt();
                error -= (blockUnitStart.Y - blockUnitStart.Y.Floor()) * absDelta.X;
            }

            for (int i = numBlocks; i >= 0; i--)
            {
                T gridElement = blocks[blockIndex];
                if (func(gridElement))
                    return true;

                // Unfortunately this algorithm will act weird on corners. If 
                // you are coming up on a corner and your error is zero, this 
                // will go along the X axis first. If you change the equality, 
                // then it will favor the Y axis. Either way... there exists a 
                // corner case. This used to cause exceptions if it occurred at
                // the origin (since it would go left instead of going up when 
                // going from cell [0, 0] to cell [0, 1] through the corner).
                //
                // Since the code is simple and very fast, the grid will just 
                // pad itself on the left-side so in the very rare literal 
                // corner case it will not go to block (-1, 0) and avoid 
                // throwing an exception by being out of bounds.
                if (error > 0)
                {
                    blockIndex += verticalStep;
                    error -= absDelta.X;
                }
                else
                {
                    blockIndex += horizontalStep;
                    error += absDelta.Y;
                }
            }

            return false;
        }
    }
}
