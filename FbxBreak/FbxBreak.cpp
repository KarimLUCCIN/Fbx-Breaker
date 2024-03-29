// This is the main DLL file.

#include "stdafx.h"
#include "FbxBreak.h"

#pragma comment(lib, "Advapi32.lib")
#pragma comment(lib, "Wininet.lib")

#ifdef DEBUG
#pragma comment(lib, "fbxsdk-2012.2d.lib")
#else
#pragma comment(lib, "fbxsdk-2012.2.lib")
#endif

#include "Common.h"
#include <vcclr.h>
#include "FbxParser.h"

KFbxSdkManager* lSdkManager;
KFbxScene* lScene;

using namespace System;
using namespace System::Runtime::InteropServices;

namespace FbxBreak {
	void FbxModelBreaker::Load(String^ fbxFile)
	{
		InitializeSdkObjects(lSdkManager, lScene);

		char* str2 = (char*)(void*)Marshal::StringToHGlobalAnsi(fbxFile);
		try
		{
			if(!LoadScene(lSdkManager, lScene, str2))
				throw gcnew System::Exception("Unable to load the scene");
		}
		finally
		{
			Marshal::FreeHGlobal((IntPtr)str2);
		}
	}

	FbxModelBreaker::FbxModelBreaker(String^ fbxFilePath)
	{
		unitScale = 1;
		leftHanded = false;
		swapWindingOrder = true;

		currentGeneratedMaxId = 1;
		Load(fbxFilePath);

		globalConverter = new KFbxGeometryConverter(lSdkManager);
	}

	void FbxModelBreaker::Save(BreakerSaveHandler^ saveHandler, BreakerOutputFormat outputFormat)
	{
		if(saveHandler == nullptr)
			throw gcnew ArgumentNullException("saveHandler");

		KFbxNode* lRootNode = lScene->GetRootNode();

		if(lRootNode) {
			for(int i = 0; i < lRootNode->GetChildCount(); i++)
				ProcessNode(lRootNode->GetChild(i), saveHandler, outputFormat);
		}
	}

	/**
	* Return a string-based representation based on the attribute type.
	*/
	String^ FbxModelBreaker::GetAttributeTypeName(KFbxNodeAttribute::EAttributeType type) 
	{
		switch(type) 
		{
		case KFbxNodeAttribute::eUNIDENTIFIED: return "unidentified";
		case KFbxNodeAttribute::eNULL: return "null";
		case KFbxNodeAttribute::eMARKER: return "marker";
		case KFbxNodeAttribute::eSKELETON: return "skeleton";
		case KFbxNodeAttribute::eMESH: return "mesh";
		case KFbxNodeAttribute::eNURB: return "nurb";
		case KFbxNodeAttribute::ePATCH: return "patch";
		case KFbxNodeAttribute::eCAMERA: return "camera";
		case KFbxNodeAttribute::eCAMERA_STEREO:    return "stereo";
		case KFbxNodeAttribute::eCAMERA_SWITCHER: return "camera switcher";
		case KFbxNodeAttribute::eLIGHT: return "light";
		case KFbxNodeAttribute::eOPTICAL_REFERENCE: return "optical reference";
		case KFbxNodeAttribute::eOPTICAL_MARKER: return "marker";
		case KFbxNodeAttribute::eNURBS_CURVE: return "nurbs curve";
		case KFbxNodeAttribute::eTRIM_NURBS_SURFACE: return "trim nurbs surface";
		case KFbxNodeAttribute::eBOUNDARY: return "boundary";
		case KFbxNodeAttribute::eNURBS_SURFACE: return "nurbs surface";
		case KFbxNodeAttribute::eSHAPE: return "shape";
		case KFbxNodeAttribute::eLODGROUP: return "lodgroup";
		case KFbxNodeAttribute::eSUBDIV: return "subdiv";
		default: return "unknown";
		}
	}

	void FbxModelBreaker::ProcessNode(KFbxNode* pNode, BreakerSaveHandler^ saveHandler, BreakerOutputFormat outputFormat)
	{
		globalMessages->Add(gcnew String(pNode->GetName()));

		KFbxXMatrix& globalTransform = 
			lScene->GetEvaluator()->GetNodeGlobalTransform(pNode);

		// Print the node's attributes.
		for(int i = 0; i < pNode->GetNodeAttributeCount(); i++)
		{
			KFbxNodeAttribute* att = pNode->GetNodeAttributeByIndex(i);

			globalMessages->Add(GetAttributeTypeName(att->GetAttributeType()));
			globalMessages->Add(gcnew String(att->GetName()));

			switch(att->GetAttributeType())
			{
			case KFbxNodeAttribute::eMESH:
				{
					ExportPart(gcnew String(att->GetName()), pNode, att, globalTransform, saveHandler, outputFormat);

					break;
				}
				/* TODO : Complete Here if you want to export more things than just meshes */
			}
		}

		// Recursively print the children nodes.
		for(int j = 0; j < pNode->GetChildCount(); j++)
			ProcessNode(pNode->GetChild(j), saveHandler, outputFormat);
	}

	FbxModelBreaker::~FbxModelBreaker()
	{
		delete globalConverter;
		DestroySdkObjects(lSdkManager);
	}

	void FbxModelBreaker::ExportPart(String^ baseId, KFbxNode* srcNode, KFbxNodeAttribute* att, KFbxXMatrix& globalTransform, BreakerSaveHandler^ saveHandler, BreakerOutputFormat outputFormat )
	{
		String^ fullId = baseId + (gcnew Int32(currentGeneratedMaxId))->ToString();
		currentGeneratedMaxId++;

		TransformGroup ^ transform = gcnew TransformGroup();
		
		KVecConv(globalTransform.GetQ(), transform->quaternion);
		KVecConv(globalTransform.GetT(), transform->translation);
		KVecConv(globalTransform.GetS(), transform->scale);

		String^ outputPath = saveHandler->ResolveOutputPath(fullId, transform);

		if(!String::IsNullOrEmpty(outputPath))
		{
			String^ outputPathTmp = outputPath + "tmp.fbx";

			char* resolvedFullPath = (char*)(void*)Marshal::StringToHGlobalAnsi(outputPath);
			char* resolvedFullPath_tmp = (char*)(void*)Marshal::StringToHGlobalAnsi(outputPathTmp);
			char* fullIdPtr = (char*)(void*)Marshal::StringToHGlobalAnsi(fullId);

			try
			{
				KFbxScene* partScene = KFbxScene::Create(lSdkManager, resolvedFullPath);
				KFbxNode* lChild = NULL;

				try
				{
					/* the transform is not applied because it's supposed to be the job of the saveHandler */
					lChild = KFbxNode::Create(partScene, fullIdPtr);
					KFbxMesh * lMesh = (KFbxMesh*) att;

					KFbxObject* lSourceObj = lMesh;
					KFbxObject* lCloneResult = NULL;
					KFbxObject* pOptDestionationContainer = lChild;

					// Use the clone manager to clone the scene
					KFbxCloneManager lCloneManager;
					KFbxCloneManager::CloneSet lCloneSet;

					// You can specify your own clone options here
					KFbxCloneManager::CloneSetElement lDefaultCloneOptions(
						KFbxCloneManager::sConnectToClone,
						0,
						KFbxObject::eDEEP_CLONE );

						//// Create the clone set;
						//lCloneSet.Insert( lSourceObj, lDefaultCloneOptions );

						//lCloneManager.AddDependents( lCloneSet, lSourceObj, lDefaultCloneOptions);

						//// Clone the object and retrieve it
						//lCloneManager.Clone(lCloneSet, pOptDestionationContainer);
						//KFbxCloneManager::CloneSet::RecordType* lIterator = lCloneSet.Find(lSourceObj);
						//lCloneResult = static_cast<KFbxObject*>(lIterator->GetValue().mObjectClone);
					
						lChild->AddNodeAttribute(globalConverter->TriangulateMesh( (KFbxMesh*)lMesh));

						int lMaterialCount = srcNode->GetSrcObjectCount( KFbxSurfaceMaterial::ClassId );

						for (int lCount = 0; lCount < lMaterialCount; lCount ++)
						{
							KFbxSurfaceMaterial *lMaterial = (KFbxSurfaceMaterial*)srcNode->GetSrcObject(KFbxSurfaceMaterial::ClassId, lCount);

							lSourceObj = lMaterial;
							lCloneSet.Clear();

							// Create the clone set;
							lCloneSet.Insert( lSourceObj, lDefaultCloneOptions );

							lCloneManager.AddDependents( lCloneSet, lSourceObj, lDefaultCloneOptions);

							// Clone the object and retrieve it
							lCloneManager.Clone(lCloneSet, pOptDestionationContainer);
							KFbxCloneManager::CloneSet::RecordType* lIterator = lCloneSet.Find(lSourceObj);
							lCloneResult = static_cast<KFbxObject*>(lIterator->GetValue().mObjectClone);

							lChild->AddMaterial((KFbxSurfaceMaterial*)lCloneResult);
						}

					partScene->GetRootNode()->AddChild(lChild);

					switch(outputFormat)
					{
					case BreakerOutputFormat::X:
						{
							SaveScene(lSdkManager, partScene, resolvedFullPath_tmp);

							ConvertToX(outputPathTmp, outputPath);

							File::Delete(outputPathTmp);

							break;
						}
					case BreakerOutputFormat::Fbx:
						{
							SaveScene(lSdkManager, partScene, resolvedFullPath);
							break;
						}
					default:
						throw gcnew Exception("Output format not supported");
					}
				}
				finally
				{
					if(lChild != NULL)
						lChild->RemoveNodeAttributeByIndex(0);

					partScene->Destroy();
				}
			}
			finally
			{
				Marshal::FreeHGlobal((IntPtr)resolvedFullPath);
				Marshal::FreeHGlobal((IntPtr)fullIdPtr);
				Marshal::FreeHGlobal((IntPtr)resolvedFullPath_tmp);
			}

		}
	}

	void FbxModelBreaker::ConvertToX( String^ fbx, String^ x)
	{
		FbxParser^ parser = gcnew FbxParser(fbx, zUp, unitScale);

		parser->SetSwapWindingOrder(swapWindingOrder);
		parser->SetCoordinate(leftHanded);

		FbxSkeletalNode^ root = parser->GetSkeletalHierarchy();
		FbxSkeletalNode^ model = parser->GetModel();
		FbxAnimationCollection^ col = parser->GetAnimation();

		XConverter^ conv = gcnew XConverter();
		conv->SwapWindingOrder = swapWindingOrder;
		conv->ConvertFbx2X(Path::GetDirectoryName(fbx), x, root, model, col, false, true);
	}

}
