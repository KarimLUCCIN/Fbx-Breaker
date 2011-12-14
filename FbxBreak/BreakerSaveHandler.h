#pragma once

using namespace System;
using namespace System::IO;

public ref struct Vect4
{
	double x;
	double y;
	double z;
	double w;
};

public ref struct Vect2
{
	double x;
	double y;
};

public ref struct TransformGroup
{
	Vect4 scale;
	Vect4 translation;
	Vect4 quaternion;
};

namespace FbxBreak {

	public ref class BreakerSaveHandler abstract
	{
	public:
		/*
		Begins to write the specified mesh using the specified id.
		The id is guaranteed to be unique.

		Returns null to skip that mesh.
		*/
		virtual String^ ResolveOutputPath(String^ id, TransformGroup ^ transform) = 0;
	};

}