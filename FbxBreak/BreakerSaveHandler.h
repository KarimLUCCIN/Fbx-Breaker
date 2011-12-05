#pragma once

using namespace System;
using namespace System::IO;

namespace FbxBreak {

	public ref class BreakerSaveHandler abstract
	{
	public:
		/*
		Begins to write the specified mesh using the specified id.
		The id is guarented to be unique.
		*/
		virtual void OutputMesh(String^ id, void * data, int dataLength) = 0;
	};

}