// FbxBreak.h

#pragma once

#include <fbxsdk.h>

#include "BreakerSaveHandler.h"

using namespace System;
using namespace System::IO;
using namespace System::Collections::Generic;

namespace FbxBreak {
	/* Output format for the converter */
	public enum struct BreakerOutputFormat { Fbx, X };

	public ref class FbxModelBreaker
	{
	private:
		/* Load the data of an fbx file into the class */
		void Load(String^ fbxFile);
		void ProcessNode(KFbxNode* pNode, BreakerSaveHandler^ saveHandler, BreakerOutputFormat outputFormat);
		String^ GetAttributeTypeName(KFbxNodeAttribute::EAttributeType type);
		void ExportPart(String^ baseId, KFbxNode* srcNode, KFbxNodeAttribute* att, KFbxXMatrix& globalTransform, BreakerSaveHandler^ saveHandler, BreakerOutputFormat outputFormat );

		int currentGeneratedMaxId;

		KFbxGeometryConverter * globalConverter;
	public:
		static List<String^>^ globalMessages;

		static FbxModelBreaker()
		{
			globalMessages = gcnew List<String^>();
		}

		FbxModelBreaker(String^ fbxFilePath);

		~FbxModelBreaker();

		void Save(BreakerSaveHandler^ saveHandler, BreakerOutputFormat outputFormat);
	};
}
