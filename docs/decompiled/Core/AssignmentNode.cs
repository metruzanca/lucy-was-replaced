using System;
using System.Collections.Generic;
using System.Linq;

public class AssignmentNode : Node
{
	private string op;

	public override string NodeName => "assign";

	public AssignmentNode(string op, CodeWindow func, int startIndex, int endIndex)
		: base(func, startIndex, endIndex)
	{
		this.op = op;
	}

	public AssignmentNode(string op, BoxedNodeParams boxedParams)
		: base(boxedParams)
	{
		this.op = op;
	}

	public override IEnumerable<double> Execute(ProgramState state, Execution execution, int depth)
	{
		ErrorsAndBreakpoints(state, execution, depth);
		foreach (double item in slots[1].Execute(state, execution, depth + 1))
		{
			yield return item;
		}
		Blink(state, execution);
		IPyObject returnValue = state.ReturnValue;
		state.CurrentExecutingNode = this;
		foreach (double item2 in Assign(state, execution, depth, slots[0], returnValue, op, boxedParams.codeWindow.fileName))
		{
			yield return item2;
		}
		state.ReturnValue = new PyNone();
	}

	public static IEnumerable<double> Assign(ProgramState state, Execution execution, int depth, Node lhs, IPyObject rhs, string op, string currentFileName)
	{
		if (lhs is ValueNode)
		{
			string value = ((ValueNode)lhs).value;
			IPyObject before = ((op == "=") ? null : state.CurrentScope.Evaluate(value, currentFileName).val);
			state.CurrentScope.SetVar(value, GetValueToAssign(before, rhs, op, out var execTime));
			lhs.Blink(state, execution);
			if (lhs.CheckIncrementOpCount(state, execution, 1.0 * execTime))
			{
				yield return 0.0;
			}
			yield break;
		}
		if (lhs is BinaryExprNode binaryExprNode)
		{
			foreach (double item in binaryExprNode.slots[0].Execute(state, execution, depth + 1))
			{
				yield return item;
			}
			if (binaryExprNode.op == ".")
			{
				if (!(state.ReturnValue is PyModule pyModule))
				{
					state.CurrentExecutingNode = lhs;
					throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_attribute_on_non_module", state.ReturnValue));
				}
				if (!(binaryExprNode.slots[1] is ValueNode))
				{
					throw new ExecuteException("error_invalid_const2", binaryExprNode.slots[1].boxedParams.wordStart, binaryExprNode.slots[1].boxedParams.wordEnd);
				}
				int num = ((!state.IsExpressionStatic) ? 1 : 0);
				string value2 = (binaryExprNode.slots[1] as ValueNode).value;
				IPyObject before2 = ((op == "=") ? null : pyModule.Evaluate(value2, binaryExprNode.slots[1].boxedParams.wordStart, binaryExprNode.slots[1].boxedParams.wordEnd).val);
				pyModule.SetAttribute(value2, GetValueToAssign(before2, rhs, op, out var execTime2));
				if (lhs.CheckIncrementOpCount(state, execution, 1.0 * (execTime2 + (double)num)))
				{
					yield return 0.0;
				}
				yield break;
			}
			if (binaryExprNode.op == "[]")
			{
				IPyObject returnValue = state.ReturnValue;
				if (!(returnValue is IPyIndexable indexable))
				{
					state.CurrentExecutingNode = lhs;
					throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_index_on_non_indexable", state.ReturnValue));
				}
				foreach (double item2 in binaryExprNode.slots[1].Execute(state, execution, depth + 1))
				{
					yield return item2;
				}
				InternalPySequence indices = (InternalPySequence)state.ReturnValue;
				int steps = 0;
				IPyObject before3 = ((op == "=") ? null : indexable.At(indices, ref steps));
				steps = 0;
				indexable.At(indices, ref steps, GetValueToAssign(before3, rhs, op, out var execTime3));
				if (lhs.CheckIncrementOpCount(state, execution, 1.0 * ((double)steps + execTime3)))
				{
					yield return 0.0;
				}
				yield break;
			}
			state.CurrentExecutingNode = lhs;
			throw new ExecuteException("error_assign_type_mismatch");
		}
		if (lhs is ListNode || lhs is TupleNode)
		{
			if (!(rhs is IPySequence))
			{
				throw new ExecuteException("error_assign_type_mismatch");
			}
			List<Node> leftNodes = lhs.slots[0].slots;
			if (leftNodes.Count < ((IPySequence)rhs).Count)
			{
				throw new ExecuteException("error_too_many_values_to_unpack");
			}
			if (leftNodes.Count > ((IPySequence)rhs).Count)
			{
				throw new ExecuteException("error_not_enough_values");
			}
			List<IPyObject> rightValues = Enumerable.ToList<IPyObject>((IEnumerable<IPyObject>)(IPySequence)rhs);
			for (int i = 0; i < leftNodes.Count; i++)
			{
				foreach (double item3 in Assign(state, execution, depth, leftNodes[i], rightValues[i], op, currentFileName))
				{
					yield return item3;
				}
			}
			yield break;
		}
		if (lhs is BracketNode)
		{
			foreach (double item4 in Assign(state, execution, depth, lhs.slots[0], rhs, op, currentFileName))
			{
				yield return item4;
			}
			yield break;
		}
		throw new ExecuteException("error_assign_type_mismatch");
	}

	private static IPyObject GetValueToAssign(IPyObject before, IPyObject rhsValue, string op, out double execTime)
	{
		execTime = 0.0;
		switch (op)
		{
		case "+=":
			if ((!(before is PyNumber) || !(rhsValue is PyNumber)) && (!(before is PyList) || !(rhsValue is IEnumerable<object>)) && (!(before is PyTuple) || !(rhsValue is PyTuple)) && (!(before is PyString) || !(rhsValue is PyString)))
			{
				throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_bad_bin_operator", op, before, rhsValue));
			}
			break;
		case "/":
		case "//":
			if (rhsValue is PyNumber pyNumber && (double)pyNumber == 0.0)
			{
				throw new ExecuteException("error_division_by_zero");
			}
			goto default;
		default:
			if (!(rhsValue is PyNumber) || !(before is PyNumber))
			{
				throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_arith_assign_not_used_on_number", op));
			}
			break;
		case "=":
			break;
		}
		IPyObject pyObject = null;
		switch (op)
		{
		case "=":
			pyObject = rhsValue;
			execTime = 0.0;
			break;
		case "+=":
			if (before is PyNumber)
			{
				pyObject = new PyNumber((double)(PyNumber)before + (double)(PyNumber)rhsValue);
				execTime = 1.0;
			}
			else if (before is PyList)
			{
				((PyList)before).list.AddRange((IEnumerable<IPyObject>)rhsValue);
				pyObject = before;
				execTime = Enumerable.Count<IPyObject>((IEnumerable<IPyObject>)rhsValue);
			}
			else if (before is PyString)
			{
				pyObject = new PyString(((PyString)before).str + ((PyString)rhsValue).str);
				execTime = ((PyString)pyObject).Count;
			}
			else if (before is PyTuple)
			{
				pyObject = new PyTuple(Enumerable.ToList<IPyObject>(Enumerable.Concat<IPyObject>((IEnumerable<IPyObject>)(PyTuple)before, (IEnumerable<IPyObject>)(PyTuple)rhsValue)));
				execTime = ((PyTuple)pyObject).Count;
			}
			break;
		case "-=":
			pyObject = new PyNumber((double)(PyNumber)before - (double)(PyNumber)rhsValue);
			execTime = 1.0;
			break;
		case "*=":
			pyObject = new PyNumber((double)(PyNumber)before * (double)(PyNumber)rhsValue);
			execTime = 1.0;
			break;
		case "/=":
			pyObject = new PyNumber((double)(PyNumber)before / (double)(PyNumber)rhsValue);
			execTime = 1.0;
			break;
		case "//=":
			pyObject = PyNumber.FloorDivision((PyNumber)before, (PyNumber)rhsValue);
			execTime = 1.0;
			break;
		case "%=":
			pyObject = PyNumber.Modulo((PyNumber)before, (PyNumber)rhsValue);
			execTime = 1.0;
			break;
		}
		return pyObject;
	}

	private IPyObject IndexInto(IPyObject indexable, IPyObject indexObject, IPyObject valueToSet = null, bool setValue = false)
	{
		if (indexable is PyList)
		{
			PyList pyList = (PyList)indexable;
			if (!(indexObject is PyNumber))
			{
				throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_index", indexObject, indexable));
			}
			int num = (int)(double)(PyNumber)indexObject;
			if (num < 0)
			{
				num = pyList.Count + num;
			}
			if (num < 0 || num >= pyList.Count)
			{
				throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_index_out_of_bounds", num, pyList));
			}
			if (setValue)
			{
				pyList[num] = valueToSet;
			}
			return pyList[num];
		}
		if (indexable is PyDict)
		{
			PyDict pyDict = (PyDict)indexable;
			if (indexObject is PyDict || indexObject is PyList || indexObject is PySet || indexObject is PyModule)
			{
				throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_bad_key", indexObject));
			}
			if (setValue)
			{
				if (pyDict.dict.ContainsKey(indexObject))
				{
					pyDict.dict[indexObject].obj = valueToSet;
				}
				else
				{
					pyDict.dict[indexObject] = new PyObjectBox(valueToSet);
				}
			}
			return pyDict.dict.GetValueOrDefault(indexObject)?.obj;
		}
		if (indexable is PyTuple)
		{
			throw new ExecuteException("error_index_on_tuple");
		}
		throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_index", indexObject, indexable));
	}

	public override void CheckDependencies(ProgramState state, Execution execution)
	{
		state.currentDependencies.Add(("variables", boxedParams.wordStart, boxedParams.wordEnd));
	}

	public override Node DeepCopy(Dictionary<object, object> copies)
	{
		if (copies.TryGetValue(this, out var value))
		{
			return (Node)value;
		}
		copies[this] = new AssignmentNode(op, boxedParams)
		{
			slots = Enumerable.ToList<Node>(Enumerable.Select<Node, Node>((IEnumerable<Node>)slots, (Func<Node, Node>)((Node s) => s.DeepCopy(copies))))
		};
		return (Node)copies[this];
	}
}
