﻿//
// Copyright (c) 2017 Lee P. Richardson
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xamarin.Forms;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace EasyLayout.Forms
{
    public static class EasyLayoutForms
    {
        private class Assertion
        {
            private double _relativeSizeDelta;

            public Assertion(View view) => View = view;
            public View View { get; }
            private LeftExpression LeftExpression { get; set; }
            private RightExpression RightExpression { get; set; }
            public string LeftName => LeftExpression.Name;

            public void Initialize(LeftExpression leftExpression, RightExpression rightExpression)
            {
                LeftExpression = leftExpression;
                RightExpression = rightExpression;
            }

            public Constraint GetSizeConstraint(Position position)
            {
                if (RightExpression.IsConstant && RightExpression.Constant.HasValue)
                    return Constraint.Constant(RightExpression.Constant.Value);

                var sibling = RightExpression.View;
                var margin = GetMargin();
                var isParent = RightExpression.IsParent;

                if (isParent && position == Position.Width)
                    return Constraint.RelativeToParent(rl => rl.Width + margin);
                if (isParent && position == Position.Height)
                    return Constraint.RelativeToParent(rl => rl.Height + margin);
                if (position == Position.Width)
                    return Constraint.RelativeToView(sibling, (rv, v) => v.Width);
                if (position == Position.Height)
                    return Constraint.RelativeToView(sibling, (rv, v) => v.Height);
                throw new ArgumentException("Unsupported position in size constraint: " + position);
            }

            public Constraint GetPositionConstraint(Assertion widthHeightAssertion = null)
            {
                return RightExpression.IsParent
                    ? GetLayoutRuleForParent(widthHeightAssertion)
                    : GetLayoutRuleForSibling(widthHeightAssertion);
            }

            private static double GetWidth(RelativeLayout relativeLayout, Guid id, bool isSizeRelativeToParent,
                double? widthAssertion)
            {
                if (isSizeRelativeToParent) return relativeLayout.Width + (widthAssertion ?? 0);
                if (widthAssertion.HasValue && widthAssertion > 0) return widthAssertion.Value;
                var child = GetChildById(relativeLayout, id);
                if (child == null) return 0;
                // -1 = width hasn't been computed yet so we have to calculate it
                return child.Width != -1 ? child.Width : GetSize(relativeLayout, child).Width;
            }

            private static double GetHeight(RelativeLayout relativeLayout, Guid id, double? heightAssertion)
            {
                if (heightAssertion.HasValue && heightAssertion > 0) return heightAssertion.Value;
                var child = GetChildById(relativeLayout, id);
                if (child == null) return 0;
                // -1 = height hasn't been computed yet so we have to calculate it
                return child.Height != -1 ? child.Height : GetSize(relativeLayout, child).Height;
            }

            /// <summary>
            /// Thought getting the height/width of an element in a constraint would be easy?  Hah.
            /// http://stackoverflow.com/questions/40942691/xamarin-forms-how-to-center-views-using-relative-layout-width-and-height-r
            /// </summary>
            private static Size GetSize(RelativeLayout relativeLayout, VisualElement visualElement)
            {
                return visualElement == null
                    ? Size.Zero
                    : (relativeLayout == null
                        ? Size.Zero
                        : visualElement.Measure(relativeLayout.Width, relativeLayout.Height).Request);
            }

            private static View GetChildById(RelativeLayout relativeLayout, Guid id) => relativeLayout.Children
                .FirstOrDefault(i => i.Id == id);

            private Constraint GetLayoutRuleForParent(Assertion sizeAssertion)
            {
                var childPosition = LeftExpression.Position;
                var parentPosition = RightExpression.Position;
                var childName = LeftExpression.Name;
                var childId = LeftExpression.View.Id;
                var margin = GetMargin();
                var sizeConstant = sizeAssertion?.RightExpression.Constant;
                var isSizeRelativeToParent = sizeAssertion?.RightExpression.IsParent ?? false;

                // X Constraints

                // aka LayoutRules.AlignParentLeft
                if (childPosition == Position.Left && parentPosition == Position.Left)
                    return Constraint.RelativeToParent(rl => rl.X + margin);

                // aka LayoutRules.AlignParentRight
                if (childPosition == Position.Right && parentPosition == Position.Right)
                    return Constraint.RelativeToParent(
                        rl => rl.Width - GetWidth(rl, childId, isSizeRelativeToParent, sizeConstant) + margin);

                // aka LayoutRules.CenterHorizontal
                if (childPosition == Position.CenterX && parentPosition == Position.CenterX)
                    return Constraint.RelativeToParent(
                        rl => rl.Width * .5f - GetWidth(rl, childId, isSizeRelativeToParent, sizeConstant) * .5f +
                              margin);

                // Y Constraints

                // aka LayoutRules.AlignParentTop
                if (childPosition == Position.Top && parentPosition == Position.Top)
                    return Constraint.RelativeToParent(rl => rl.Y + margin);

                // aka LayoutRules.AlignParentBottom
                if (childPosition == Position.Bottom && parentPosition == Position.Bottom)
                    return Constraint.RelativeToParent(rl => rl.Height - GetHeight(rl, childId, sizeConstant) + margin);

                // aka LayoutRules.CenterVertical
                if (childPosition == Position.CenterY && parentPosition == Position.CenterY)
                    return Constraint.RelativeToParent(
                        rl => rl.Height * .5f - GetHeight(rl, childId, sizeConstant) * .5f + margin);

                // todo: support more parent layout constraints
                // aka LayoutRules.CenterInParent
                //if (childPosition == Position.Center && parentPosition == Position.Center)
                //    return LayoutRules.CenterInParent;
                //if (childPosition == Position.Width && parentPosition == Position.Width)
                //    return null;
                //if (childPosition == Position.Height && parentPosition == Position.Height)
                //    return null;
                throw new Exception(
                    $"Unsupported parent positioning combination: {childName}.{childPosition} with parent.{parentPosition}");
            }

            private Constraint GetLayoutRuleForSibling(Assertion widthHeightAssertion)
            {
                var leftPosition = LeftExpression.Position;
                var rightPosition = RightExpression.Position;
                var leftExpressionName = LeftExpression.Name;
                var rightExpressionName = RightExpression.Name;
                var sibling = RightExpression.View;
                var margin = GetMargin();
                var childId = LeftExpression.View.Id;
                var heightWidthConstant = widthHeightAssertion?.RightExpression.Constant;

                // X Constraints

                // aka LayoutRules.AlignLeft
                if (leftPosition == Position.Left && rightPosition == Position.Left)
                    return Constraint.RelativeToView(sibling, (rl, v) => v.X + margin);

                // aka LayoutRules.AlignRight
                if (leftPosition == Position.Right && rightPosition == Position.Right)
                    return Constraint.RelativeToView(sibling,
                        (rl, v) => v.Bounds.Right - GetWidth(rl, childId, false, heightWidthConstant) + margin);

                // aka LayoutRules.RightOf
                if (leftPosition == Position.Left && rightPosition == Position.Right)
                    return Constraint.RelativeToView(sibling, (rl, v) => v.Bounds.Right + margin);

                // aka LayoutRules.LeftOf
                if (leftPosition == Position.Right && rightPosition == Position.Left)
                    return Constraint.RelativeToView(sibling,
                        (rl, v) => v.Bounds.Left - GetWidth(rl, childId, false, heightWidthConstant) + margin);

                // Y Constraints

                // aka LayoutRules.AlignTop
                if (leftPosition == Position.Top && rightPosition == Position.Top)
                    return Constraint.RelativeToView(sibling, (rl, v) => v.Y + margin);

                // aka LayoutRules.AlignBottom
                if (leftPosition == Position.Bottom && rightPosition == Position.Bottom)
                    return Constraint.RelativeToView(sibling,
                        (rl, v) => v.Bounds.Bottom - GetHeight(rl, childId, heightWidthConstant) + margin);

                // aka LayoutRules.Below
                if (leftPosition == Position.Top && rightPosition == Position.Bottom)
                    return Constraint.RelativeToView(sibling, (rl, v) => v.Bounds.Bottom + margin);

                // aka LayoutRules.Above
                if (leftPosition == Position.Bottom && rightPosition == Position.Top)
                    return Constraint.RelativeToView(sibling,
                        (rl, v) => v.Bounds.Top - GetHeight(rl, childId, heightWidthConstant) + margin);

                if (leftPosition == Position.CenterX && rightPosition == Position.CenterX)
                    return Constraint.RelativeToView(sibling,
                        (rl, v) => v.X + (v.Bounds.Width * .5f) -
                                   GetWidth(rl, childId, false, heightWidthConstant) * .5f + margin);

                // todo: constrain width's & height's
                //if (leftPosition == Position.Width && rightPosition == Position.Width)
                //    throw new ArgumentException("Unfortunatly Android's relative layout isn't sophisticated enough to allow constraining widths.  You might be able to achieve the same result by constraining Left's and Right's.");
                //if (leftPosition == Position.Height && rightPosition == Position.Height)
                //    throw new ArgumentException("Unfortunatly Android's relative layout isn't sophisticated enough to allow constraining heights.  You might be able to achieve the same result by constraining Top's and Bottom's.");
                throw new ArgumentException(
                    $"Unsupported relative positioning combination: {leftExpressionName}.{leftPosition} with {rightExpressionName}.{rightPosition}");
            }

            private double GetMargin()
            {
                if (RightExpression.Constant == null) return 0 - _relativeSizeDelta;
                var value = RightExpression.Constant.Value;

                switch (LeftExpression.Position)
                {
                    case Position.Top:
                    case Position.Left:
                    case Position.Right:
                    case Position.Bottom:
                    case Position.Width:
                    case Position.Height:
                    case Position.CenterX:
                        return value - _relativeSizeDelta;
                    default:
                        throw new ArgumentException(
                            $"Constant expressions with {RightExpression.Position} are currently unsupported.");
                }
            }

            public bool IsExplicitWidthAssertion()
            {
                return LeftExpression.Position == Position.Width;
            }

            public bool IsExplicitHeightAssertion()
            {
                return LeftExpression.Position == Position.Height;
            }

            public bool IsXConstraint()
            {
                return LeftExpression.Position == Position.Left ||
                       LeftExpression.Position == Position.Right ||
                       LeftExpression.Position == Position.CenterX;
            }

            public bool IsYConstraint()
            {
                return LeftExpression.Position == Position.Top ||
                       LeftExpression.Position == Position.Bottom ||
                       LeftExpression.Position == Position.CenterY;
            }

            public bool IsTopOrLeft()
            {
                return LeftExpression.Position == Position.Top ||
                       LeftExpression.Position == Position.Left;
            }

            public Position Position => LeftExpression.Position;

            public void UpdateMarginFromRelativeWidthConstraint(Assertion leftOrTopAssertion)
            {
                _relativeSizeDelta = leftOrTopAssertion?.RightExpression.Constant ?? 0;
            }
        }

        private enum Position
        {
            Top,
            Right,
            Left,
            Bottom,
            CenterX,
            CenterY,
            Constant,
            Width,
            Height
        }

        private struct LeftExpression
        {
            public View View { get; set; }
            public Position Position { get; set; }
            public string Name { get; set; }
        }

        private struct RightExpression
        {
            public bool IsParent { get; set; }
            public View View { get; set; }
            public string Name { get; set; }
            public Position Position { get; set; }
            public double? Constant { get; set; }
            public bool IsConstant => !IsParent && View == null && Constant != null;
        }

        public static int ToConst(this int i)
        {
            return i;
        }

        private static int ToConst(this float i)
        {
            return (int) i;
        }

        public static void ConstrainLayout(this RelativeLayout relativeLayout, Expression<Func<bool>> constraints)
        {
            var constraintExpressions = FindConstraints(constraints.Body);
            var viewAndRule = ConvertConstraintsToRules(relativeLayout, constraintExpressions);
            UpdateLayoutParamsWithRules(relativeLayout, viewAndRule);
        }

        private static void UpdateLayoutParamsWithRules(RelativeLayout relativeLayout,
            IEnumerable<Assertion> viewAndRule)
        {
            var assertionsGroupedByView = viewAndRule.GroupBy(i => i.View);

            foreach (var viewAndRules in assertionsGroupedByView)
            {
                var view = viewAndRules.Key;
                var widthAssertion = GetExplicitWidthConstraint(viewAndRules);
                var heightAssertion = GetExplicitHeightConstraint(viewAndRules);
                var xConstraint = GetXConstraint(viewAndRules, widthAssertion);
                var yConstraint = GetYConstraint(viewAndRules, heightAssertion);
                widthAssertion = widthAssertion ?? GetRelativeWidthConstraint(viewAndRules);
                heightAssertion = heightAssertion ?? GetRelativeHeightConstraint(viewAndRules);
                var widthConstraint = widthAssertion?.GetSizeConstraint(Position.Width);
                var heightConstraint = heightAssertion?.GetSizeConstraint(Position.Height);

                relativeLayout.Children.Add(view, xConstraint, yConstraint, widthConstraint, heightConstraint);
            }
        }

        private static Constraint GetXConstraint(IGrouping<View, Assertion> assertions, Assertion widthAssertion)
        {
            return GetXorYConstraint(assertions, widthAssertion, "X", i => i.IsXConstraint());
        }


        private static Constraint GetXorYConstraint(IGrouping<View, Assertion> assertions,
            Assertion sizeAssertion, string xorY,
            Func<Assertion, bool> positionConstraintFunc)
        {
            var xAssertions = assertions.Where(positionConstraintFunc).ToList();
            switch (xAssertions.Count)
            {
                case 0:
                    return null;
                case 1:
                    return xAssertions[0].GetPositionConstraint(sizeAssertion);
                case 2:
                    var assertion = GetLeftOrTop(xAssertions[0], xAssertions[1]);
                    return assertion.GetPositionConstraint(sizeAssertion);
                default:
                    throw new ArgumentException($"Multiple assertions found affecting {xorY} for " +
                                                xAssertions[0].LeftName);
            }
        }

        private static Assertion GetLeftOrTop(Assertion assertion1, Assertion assertion2)
        {
            if (assertion1.IsTopOrLeft()) return assertion1;
            if (assertion2.IsTopOrLeft()) return assertion2;
            throw new ArgumentException("There are two statements for " + assertion1.LeftName +
                                        " affecting X or Y.  To make this work one of them must be Top or Left.");
        }

        private static Constraint GetYConstraint(IGrouping<View, Assertion> assertions, Assertion heightAssertion)
        {
            return GetXorYConstraint(assertions, heightAssertion, "Y", i => i.IsYConstraint());
        }

        private static Assertion GetExplicitHeightConstraint(IGrouping<View, Assertion> assertions)
        {
            return GetSingleAssertionOrDefault(assertions, i => i.IsExplicitHeightAssertion(),
                "Multiple height assertions found");
        }

        private static Assertion GetExplicitWidthConstraint(IGrouping<View, Assertion> assertions)
        {
            return GetSingleAssertionOrDefault(assertions, i => i.IsExplicitWidthAssertion(),
                "Multiple width assertions found");
        }

        private static Assertion GetRelativeHeightConstraint(IGrouping<View, Assertion> assertions)
        {
            var top = GetSingleAssertionOrDefault(assertions, i => i.Position == Position.Top,
                "Only one top assertion allowed");
            var bottom = GetSingleAssertionOrDefault(assertions, i => i.Position == Position.Bottom,
                "Only one bottom assertion allowed");
            if (top == null || bottom == null) return null;
            return bottom;
        }

        private static Assertion GetRelativeWidthConstraint(IGrouping<View, Assertion> assertions)
        {
            var left = GetSingleAssertionOrDefault(assertions, i => i.Position == Position.Left,
                "Only one left assertion allowed");
            var right = GetSingleAssertionOrDefault(assertions, i => i.Position == Position.Right,
                "Only one left assertion allowed");
            if (left == null || right == null) return null;
            right.UpdateMarginFromRelativeWidthConstraint(left);
            return right;
        }

        private static Assertion GetSingleAssertionOrDefault(IGrouping<View, Assertion> viewsGroupedByRule,
            Func<Assertion, bool> func, string errorMessage)
        {
            var assertions = viewsGroupedByRule.Where(func).ToList();
            return assertions.Count > 1
                ? throw new ArgumentException(errorMessage + ".  Element name: " + viewsGroupedByRule.First().LeftName)
                : (assertions.Count == 0 ? null : assertions[0]);
        }

        private static IEnumerable<Assertion> ConvertConstraintsToRules(RelativeLayout relativeLayout,
            IEnumerable<BinaryExpression> constraintExpressions)
        {
            return constraintExpressions.Select(i => GetViewAndRule(i, relativeLayout));
        }

        private static Assertion GetViewAndRule(BinaryExpression expr, RelativeLayout relativeLayout)
        {
            var leftExpression = ParseLeftExpression(expr.Left);
            var rightExpression = ParseRightExpression(expr.Right, relativeLayout);
            return GetRule(leftExpression, rightExpression);
        }

        private static Assertion GetRule(LeftExpression leftExpression, RightExpression rightExpression)
        {
            var rule = new Assertion(leftExpression.View);
            rule.Initialize(leftExpression, rightExpression);
            return rule;
        }

        private static RightExpression ParseRightExpression(Expression expr, RelativeLayout relativeLayout)
        {
            Position? position = null;
            Expression memberExpression = null;
            int? constant = null;

            if (expr.NodeType == ExpressionType.Add || expr.NodeType == ExpressionType.Subtract)
            {
                var rb = (BinaryExpression) expr;
                if (IsConstant(rb.Left))
                {
                    throw new ArgumentException(
                        "Addition and substraction are only supported when there is a view on the left and a constant on the right");
                }
                if (IsConstant(rb.Right))
                {
                    constant = ConstantValue(rb.Right);
                    if (expr.NodeType == ExpressionType.Subtract)
                    {
                        constant = -constant;
                    }
                    expr = rb.Left;
                }
                else
                {
                    throw new NotSupportedException("Addition only supports constants: " + rb.Right.NodeType);
                }
            }

            if (IsConstant(expr))
            {
                position = Position.Constant;
                constant = ConstantValue(expr);
            }
            else
            {
                if (expr is MethodCallExpression fExpr)
                {
                    position = GetPosition(fExpr);
                    memberExpression = fExpr.Arguments.FirstOrDefault() as MemberExpression;
                }
            }

            var view = GetView(memberExpression);
            var memberName = GetName(memberExpression);
            var isParent = view == relativeLayout;

            return new RightExpression
            {
                IsParent = isParent,
                View = view,
                Position = position.Value,
                Name = memberName,
                Constant = constant
            };
        }

        private static string GetName(Expression expression)
        {
            if (expression is MemberExpression memberExpression)
                return memberExpression.Member.Name;
            return expression?.ToString();
        }

        private static View GetView(Expression viewExpr)
        {
            if (viewExpr == null) return null;
            if (viewExpr is MemberExpression memberExpression)
            {
                if (memberExpression.Expression is MemberExpression viewExpression)
                {
                    var eval = Eval(viewExpression);
                    var view = eval as View;
                    if (view == null)
                        throw new NotSupportedException("Constraints only apply to View's.");
                    return view;
                }
                return Eval(memberExpression) as View;
            }
            throw new NotSupportedException("Constraints only apply to View.Frame.");
        }

        private static int ConstantValue(Expression expr)
        {
            return Convert.ToInt32(Eval(expr));
        }

        private static bool IsConstant(Expression expr)
        {
            switch (expr)
            {
                case ConstantExpression _:
                    return true;
                case MemberExpression mexpr:
                    // todo: Does this even work, where the heck is MemberType?
                    return mexpr.NodeType == ExpressionType.Constant;
                case UnaryExpression cexpr:
                    return IsConstant(cexpr.Operand);
                case MethodCallExpression methodCall:
                    return methodCall.Method.Name == nameof(ToConst);
                default:
                    return false;
            }
        }

        private static LeftExpression ParseLeftExpression(Expression expr)
        {
            var fExpr = expr as MethodCallExpression;
            var viewExpr = fExpr?.Arguments.FirstOrDefault() as MemberExpression;
            return new LeftExpression
            {
                View = GetView(viewExpr),
                Position = GetPosition(fExpr),
                Name = viewExpr?.Member.Name
            };
        }

        private static object Eval(Expression expr)
        {
            if (expr is ConstantExpression constExpr)
                return constExpr.Value;

            if (expr is MemberExpression mexpr)
                //todo: Where the heck is MemberType?
                //if (m.MemberType == MemberTypes.Field)
                //{
                if (mexpr.Member is FieldInfo fieldInfo)
                    return fieldInfo.GetValue(Eval(mexpr.Expression));

            if (expr.NodeType == ExpressionType.Convert && expr is UnaryExpression cexpr)
            {
                var op = Eval(cexpr.Operand);
                return cexpr.Method != null
                    ? cexpr.Method.Invoke(null, new[] {op})
                    : Convert.ChangeType(op, cexpr.Type);
            }

            return Expression.Lambda(expr).Compile().DynamicInvoke();
        }

        private static Position GetPosition(MethodCallExpression fExpr)
        {
            switch (fExpr.Method.Name)
            {
                case nameof(Top):
                    return Position.Top;
                case nameof(Y):
                    return Position.Top;
                case nameof(Left):
                    return Position.Left;
                case nameof(X):
                    return Position.Left;
                case nameof(Right):
                    return Position.Right;
                case nameof(Bottom):
                    return Position.Bottom;
                case nameof(Height):
                    return Position.Height;
                case nameof(Width):
                    return Position.Width;
                case nameof(CenterX):
                    return Position.CenterX;
                case nameof(CenterY):
                    return Position.CenterY;
                default:
                    throw new NotSupportedException("Method " + fExpr.Method.Name + " is not recognized.");
            }
        }

        private static IEnumerable<BinaryExpression> FindConstraints(Expression expr)
        {
            var binaryExpressions = new List<BinaryExpression>();
            FindConstraints(expr, binaryExpressions);
            return binaryExpressions;
        }

        private static void FindConstraints(Expression expr, ICollection<BinaryExpression> constraintExprs)
        {
            if (expr is BinaryExpression bExpr)
            {
                if (bExpr.NodeType == ExpressionType.AndAlso)
                {
                    FindConstraints(bExpr.Left, constraintExprs);
                    FindConstraints(bExpr.Right, constraintExprs);
                }
                else
                {
                    constraintExprs.Add(bExpr);
                }
            }
        }

        // Helper extension methods to make it easier to do the relative layout
        public static int Top(this VisualElement visualElement)
        {
            return 0;
        }

        public static int Y(this VisualElement visualElement)
        {
            return 0;
        }

        public static int Left(this VisualElement visualElement)
        {
            return 0;
        }

        public static int X(this VisualElement visualElement)
        {
            return 0;
        }

        public static int Right(this VisualElement visualElement)
        {
            return 0;
        }

        public static int Bottom(this VisualElement visualElement)
        {
            return 0;
        }

        public static int CenterX(this VisualElement visualElement)
        {
            return 0;
        }

        public static int CenterY(this VisualElement visualElement)
        {
            return 0;
        }
        
        public static int Height(this VisualElement visualElement)
        {
            return 0;
        }

        public static int Width(this VisualElement visualElement)
        {
            return 0;
        }
    }
}