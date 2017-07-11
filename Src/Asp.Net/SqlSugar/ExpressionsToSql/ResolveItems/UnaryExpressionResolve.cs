﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
namespace SqlSugar
{
    public class UnaryExpressionResolve : BaseResolve
    {
        public UnaryExpressionResolve(ExpressionParameter parameter) : base(parameter)
        {
            var expression = base.Expression as UnaryExpression;
            var baseParameter = parameter.BaseParameter;
            switch (this.Context.ResolveType)
            {
                case ResolveExpressType.WhereSingle:
                case ResolveExpressType.WhereMultiple:
                case ResolveExpressType.FieldSingle:
                case ResolveExpressType.FieldMultiple:
                case ResolveExpressType.SelectSingle:
                case ResolveExpressType.SelectMultiple:
                case ResolveExpressType.Update:
                    var nodeType = expression.NodeType;
                    base.Expression = expression.Operand;
                    var isMember = expression.Operand is MemberExpression;
                    var isConst = expression.Operand is ConstantExpression;
                    if (baseParameter.CurrentExpression is NewArrayExpression)
                    {
                        Result(parameter, nodeType);
                    }
                    else if (base.Expression is BinaryExpression || parameter.BaseExpression is BinaryExpression || baseParameter.CommonTempData.ObjToString() == CommonTempDataType.Append.ToString())
                    {
                        Append(parameter, nodeType);
                    }
                    else if (isMember)
                    {
                        MemberLogic(parameter, baseParameter, nodeType);
                    }
                    else if (isConst)
                    {
                        Result(parameter, nodeType);
                    }
                    else
                    {
                        Append(parameter, nodeType);
                    }
                    break;
                default:
                    break;
            }
        }

        private void MemberLogic(ExpressionParameter parameter, ExpressionParameter baseParameter, ExpressionType nodeType)
        {
            var memberExpression = (base.Expression as MemberExpression);
            var isLogicOperator = ExpressionTool.IsLogicOperator(baseParameter.OperatorValue) || baseParameter.OperatorValue.IsNullOrEmpty();
            var isHasValue = isLogicOperator && memberExpression.Member.Name == "HasValue" && memberExpression.Expression != null && memberExpression.NodeType == ExpressionType.MemberAccess;
            if (isHasValue)
            {
                var member = memberExpression.Expression as MemberExpression;
                parameter.CommonTempData = CommonTempDataType.Result;
                var isConst = member.Expression != null && member.Expression is ConstantExpression;
                this.Expression = isConst?member.Expression:member;
                this.Start();
                var methodParamter = isConst ? new MethodCallExpressionArgs() { IsMember=false } : new MethodCallExpressionArgs() { IsMember = true, MemberName = parameter.CommonTempData, MemberValue = null };
                var result = this.Context.DbMehtods.HasValue(new MethodCallExpressionModel()
                {
                    Args = new List<MethodCallExpressionArgs>() {
                      methodParamter
                  }
                });
                this.Context.Result.Append(result);
                parameter.CommonTempData = null;
            }
            else if (memberExpression.Type == PubConst.BoolType && isLogicOperator)
            {
                Append(parameter, nodeType);
            }
            else
            {
                Result(parameter, nodeType);
            }
        }

        private void Result(ExpressionParameter parameter, ExpressionType nodeType)
        {
            BaseParameter.ChildExpression = base.Expression;
            parameter.CommonTempData = CommonTempDataType.Result;
            if (nodeType == ExpressionType.Not)
                AppendNot(parameter.CommonTempData);
            base.Start();
            parameter.BaseParameter.CommonTempData = parameter.CommonTempData;
            parameter.BaseParameter.ChildExpression = base.Expression;
            parameter.CommonTempData = null;
        }

        private void Append(ExpressionParameter parameter, ExpressionType nodeType)
        {
            BaseParameter.ChildExpression = base.Expression;
            this.IsLeft = parameter.IsLeft;
            parameter.CommonTempData = CommonTempDataType.Append;
            if (nodeType == ExpressionType.Not)
                AppendNot(parameter.CommonTempData);
            base.Start();
            parameter.BaseParameter.CommonTempData = parameter.CommonTempData;
            parameter.BaseParameter.ChildExpression = base.Expression;
            parameter.CommonTempData = null;
        }
    }
}
