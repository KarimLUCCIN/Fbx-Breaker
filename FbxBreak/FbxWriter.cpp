#include "StdAfx.h"
#include "FbxWriter.h"

#include "Common.h"
#include <vcclr.h>

using namespace System;
using namespace System::Runtime::InteropServices;

namespace FbxBreak {

	FbxWriter::FbxWriter(void)
	{
		KFbxSdkManager * _gSdkManager;
		KFbxScene * _gScene;

		InitializeSdkObjects(_gSdkManager, _gScene);

		gSdkManager = _gSdkManager;
		gScene = _gScene;
	}

	FbxWriter::~FbxWriter()
	{
		try 
		{
			DestroySdkObjects(gSdkManager);
		}
		catch(...) {}
	}

	void FbxWriter::Save( String^ path )
	{
		char* resolvedFullPath = (char*)(void*)Marshal::StringToHGlobalAnsi(path);

		try
		{
			SaveScene(gSdkManager, gScene, resolvedFullPath);
		}
		finally
		{
			Marshal::FreeHGlobal((IntPtr)resolvedFullPath);
		}
	}

	KFbxVector4 KParseVector4(Vect4^ p, double factor) 
	{
		KFbxVector4 result(p->x * factor, p->y * factor, p->z * factor, p->w * factor);
		return result;
	}

	void FbxWriter::AppendSpline( String ^ name, array<Vect4^> ^ positions, bool closed)
	{
		if(positions == nullptr)
			throw gcnew System::ArgumentNullException("positions");
		else
		{
			KFbxNurbsCurve* lCurve;

			if(String::IsNullOrEmpty(name))
				name = String::Empty;

			char* fullName = (char*)(void*)Marshal::StringToHGlobalAnsi(name);

			try
			{
				lCurve = KFbxNurbsCurve::Create(gScene, fullName);

				lCurve->SetOrder(4);
				lCurve->InitControlPoints(closed ? positions->Length + 1 : positions->Length, closed ? KFbxNurbsCurve::eCLOSED : KFbxNurbsCurve::eOPEN);

				for (int i = 0; i < positions->Length; i++) 
				{
					lCurve->SetControlPointAt(KParseVector4(positions[i], 1), i);
				}

				if(closed)
					lCurve->SetControlPointAt(KParseVector4(positions[0], 1), positions->Length);

				int knotCount = lCurve->GetKnotCount();
				double * knots = lCurve->GetKnotVector();

				for(int i = 0;i<knotCount;i++)
				{
					knots[i] = i;
				}

				KFbxNode* lNode = KFbxNode::Create(gScene, fullName);
				lNode->SetNodeAttribute(lCurve);

				gScene->GetRootNode()->AddChild(lNode);
			}
			finally
			{
				Marshal::FreeHGlobal((IntPtr)fullName);
			}
		}
	}

}