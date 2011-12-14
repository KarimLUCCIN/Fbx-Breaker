/****************************************************************************************

   Copyright (C) 2011 Autodesk, Inc.
   All rights reserved.

   Use of this software is subject to the terms of the Autodesk license agreement
   provided at the time of installation or download, or which otherwise accompanies
   this software in either electronic or hard copy form.

****************************************************************************************/
#pragma once

#include <fbxsdk.h>
#include <stdio.h>

void InitializeSdkObjects(KFbxSdkManager*& pSdkManager, KFbxScene*& pScene);
void DestroySdkObjects(KFbxSdkManager* pSdkManager);
void CreateAndFillIOSettings(KFbxSdkManager* pSdkManager);

bool SaveScene(KFbxSdkManager* pSdkManager, KFbxDocument* pScene, const char* pFilename, int pFileFormat=-1, bool pEmbedMedia=false);
bool LoadScene(KFbxSdkManager* pSdkManager, KFbxDocument* pScene, const char* pFilename);

#define KVecConv(param1, result) \
{ \
	(result).x = (param1).mData[0]; \
	(result).y = (param1).mData[1]; \
	(result).z = (param1).mData[2]; \
	(result).w = (param1).mData[3]; \
}


