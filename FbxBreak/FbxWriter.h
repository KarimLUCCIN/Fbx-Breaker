#pragma once

#include <fbxsdk.h>
#include "BreakerSaveHandler.h"

using namespace System;
using namespace System::IO;
using namespace System::Collections::Generic;

namespace FbxBreak {

	public ref class FbxWriter
	{
	private:
		KFbxSdkManager* gSdkManager;
		KFbxScene * gScene;
	public:
		FbxWriter(void);
		~FbxWriter();

		void AppendSpline(String ^ name, array<Vect4^> ^ positions, bool closed);

		void Save(String^ path);
	};

}

