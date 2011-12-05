// ForFbx.h

#pragma once


#include <fbxsdk.h>
#include "Common.h"

using namespace System;
using namespace System::IO;
using namespace System::Runtime::InteropServices;
using namespace System::Collections::Generic;
using namespace System::Collections;
using namespace System::Collections::Specialized;
using namespace Microsoft::DirectX;
using namespace FbxBreak::XFileWriter;

extern KFbxSdkManager* lSdkManager;

namespace FbxBreak {

	public ref class FbxParser
	{

	private:
		KFbxGeometryConverter *globalConverter;
		//NodeContent^ rootNode;
		//X3DModel^ _model;
		bool Ready;
		bool DEBUG;
		//
		FbxSkeletalNode^ boneRoot ;
		FbxAnimationCollection^ animCol ;
		String^ DefaultBoneName;
		FbxSkeletalNode^ rootmodel ;

		String^ inputURL;
		String^ initialDirectory;
		bool zUP;
		double unitScale;
		bool bSwapWindingOrder;
		bool bLeftHand;
		//Color^ colorFromRgb(KFbxColor rgb)
		//{
		//	Color^color=gcnew Color();
		//	double r = Math::Floor( rgb.mRed * 255 + 0.5);
		//	double g = Math::Floor(rgb.mGreen * 255 + 0.5);
		//	double b = Math::Floor(rgb.mBlue  * 255 + 0.5);
		//	double a= Math::Floor(rgb.mAlpha  * 255 + 0.5);

		//	return gcnew Color((unsigned char)a,(unsigned char)r, (unsigned char)g, (unsigned char)b);

		//	//return color;//Color.FromRgb((byte)r, (byte)g, (byte)b);
		//}
		Vector3 Rgb2Vector3(KFbxColor rgb)
		{
			//double r = Math::Floor( rgb.mRed * 255 + 0.5);
			// double g = Math::Floor(rgb.mGreen * 255 + 0.5);
			//double b = Math::Floor(rgb.mBlue  * 255 + 0.5);
			//double a= Math::Floor(rgb.mAlpha  * 255 + 0.5);

			return Vector3((float)rgb.mRed,(float)rgb.mGreen, (float)rgb.mBlue);

			//return color;//Color.FromRgb((byte)r, (byte)g, (byte)b);
		}
		Vector4 Rgb2Vector4(KFbxColor rgb)
		{
			//double r = Math::Floor( rgb.mRed * 255 + 0.5);
			// double g = Math::Floor(rgb.mGreen * 255 + 0.5);
			//double b = Math::Floor(rgb.mBlue  * 255 + 0.5);
			//double a= Math::Floor(rgb.mAlpha  * 255 + 0.5);

			return Vector4((float)rgb.mRed,(float)rgb.mGreen, (float)rgb.mBlue,(float)rgb.mAlpha);

			//return color;//Color.FromRgb((byte)r, (byte)g, (byte)b);
		}
		Matrix FbxToMatrix(KFbxXMatrix *xmat )
		{
			Matrix mat;
			mat.M11= (float)xmat->Get(0,0);
			mat.M12= (float)xmat->Get(0,1);
			mat.M13= (float)xmat->Get(0,2);
			mat.M14= (float)xmat->Get(0,3);

			mat.M21= (float)xmat->Get(1,0);
			mat.M22= (float)xmat->Get(1,1);
			mat.M23= (float)xmat->Get(1,2);
			mat.M24= (float)xmat->Get(1,3);

			mat.M31= (float)xmat->Get(2,0);
			mat.M32= (float)xmat->Get(2,1);
			mat.M33= (float)xmat->Get(2,2);
			mat.M34= (float)xmat->Get(2,3);

			mat.M41= (float)xmat->Get(3,0);
			mat.M42= (float)xmat->Get(3,1);
			mat.M43= (float)xmat->Get(3,2);
			mat.M44= (float)xmat->Get(3,3);

			TempUnitScale(&mat);
			return mat;
		}
		void TempUnitScale(KFbxVector4 &vec)
		{
			for(int i=0;i<4;i++)
			{
				vec[i]/=this->unitScale;
			}
		}
		void TempUnitScale(Matrix *mat)
		{
			mat->M41/=this->unitScale;
			mat->M42/=this->unitScale;
			mat->M43/=this->unitScale;
		}
		KFbxXMatrix GetGeometry(KFbxNode* pNode)
		{
			KFbxVector4 lT, lR, lS;
			KFbxXMatrix lGeometry;

			lT = pNode->GetGeometricTranslation(KFbxNode::eSOURCE_SET);
			lR = pNode->GetGeometricRotation(KFbxNode::eSOURCE_SET);
			lS = pNode->GetGeometricScaling(KFbxNode::eSOURCE_SET);

			lGeometry.SetT(lT);
			lGeometry.SetR(lR);
			lGeometry.SetS(lS);

			return lGeometry;
		}
		// Get the matrix of the given pose
		KFbxXMatrix GetPoseMatrix(KFbxPose* pPose, int pNodeIndex)
		{
			KFbxXMatrix lPoseMatrix;
			KFbxMatrix lMatrix = pPose->GetMatrix(pNodeIndex);

			memcpy((double*)lPoseMatrix, (double*)lMatrix, sizeof(lMatrix.mData));

			return lPoseMatrix;
		}


		Vector4 FbxToVector4(KFbxVector4 xVec)
		{
			return Vector4((float)xVec.GetAt(0),(float)xVec.GetAt(1),(float)xVec.GetAt(2),(float)xVec.GetAt(3));
		}

		FbxSkeletalNode^ loadOneNode(KFbxNode* pNode)
		{
			return loadOneNode(pNode,nullptr);
		}
		/// only hierarchy is get if meshCache=null
		FbxSkeletalNode^ loadOneNode(KFbxNode* pNode,List<FbxMesh^>^ meshCache)
		{
			FbxSkeletalNode^ node = gcnew FbxSkeletalNode("", nullptr, Matrix::Identity);
			//
			node->NodeName=gcnew String((char *) pNode->GetName());
			//<<mesh
			if(meshCache!=nullptr)
			{
				KFbxNodeAttribute::EAttributeType lAttributeType;
				int i;

				if(pNode->GetNodeAttribute() == NULL)
				{
					//printf("NULL Node Attribute\n\n");
				}
				else
				{
					lAttributeType = (pNode->GetNodeAttribute()->GetAttributeType());

					switch (lAttributeType)
					{
					case KFbxNodeAttribute::eMARKER:  
						//DisplayMarker(pNode);
						break;

					case KFbxNodeAttribute::eSKELETON:  
						//DisplaySkeleton(pNode);
						break;

					case KFbxNodeAttribute::eMESH:      
						{
							//DisplayMesh(pNode);
							KFbxMesh * origMesh=(KFbxMesh*)pNode->GetNodeAttribute();
							KFbxMesh * pMesh= globalConverter->TriangulateMesh(origMesh);
							//NodeContent ^geo=parseMesh(pNode,pMesh);
							//pgroup->Children->Add(geo);
							FbxMesh^ geo=parseMesh(pNode,pMesh);
							if(geo!=nullptr)
							{
								node->Mesh=geo;
								meshCache->Add(geo);
							}

						}
						break;
					case KFbxNodeAttribute::eNURB:      
						{
							KFbxNurb  * origNurb=(KFbxNurb *)pNode->GetNodeAttribute();
							KFbxMesh * pMesh= globalConverter->TriangulateNurb (origNurb);
							FbxMesh^ geo=parseMesh(pNode,pMesh);
							if(geo!=nullptr)
							{
								node->Mesh=geo;
								meshCache->Add(geo);
							}
						}
						break;

					case KFbxNodeAttribute::ePATCH:     
						{
							KFbxPatch  * origPatch=(KFbxPatch *)pNode->GetNodeAttribute();
							KFbxMesh * pMesh= globalConverter->TriangulatePatch (origPatch);
							FbxMesh^ geo=parseMesh(pNode,pMesh);
							if(geo!=nullptr)
							{
								node->Mesh=geo;
								meshCache->Add(geo);
							}
						}
						break;

					case KFbxNodeAttribute::eCAMERA:    
						//DisplayCamera(pNode);
						//dealCamera((KFbxCamera*)pNode->GetNodeAttribute());
						break;

					case KFbxNodeAttribute::eLIGHT:     
						//DisplayLight(pNode);
						//Light^ light=parseLight((KFbxLight*)pNode->GetNodeAttribute());
						//if(light!=nullptr)
						//{
						//	pgroup->Children->Add(light);
						//}
						break;
					}   
				}
			}
			//>>mesh





			for(int i = 0; i < pNode->GetChildCount(); i++)
			{
				FbxSkeletalNode^ subnode= loadOneNode(pNode->GetChild(i),meshCache);
				//node->Children->Add(subnode);
				node->AddChildren(subnode);
			}
			//  else if (obj is XFrameTransformMatrix)
						
			KFbxAnimEvaluator* evaluator = 
				pNode->GetScene()->GetEvaluator();
			
			KFbxXMatrix matrix= evaluator->GetNodeGlobalTransform(pNode);
			KFbxNode* pParent=pNode->GetParent();
			if(pParent)
			{
				KFbxXMatrix matrix2= evaluator->GetNodeGlobalTransform(pParent);
				matrix=matrix2.Inverse()* matrix;
			}
			node->TransformMatrix=this->FbxToMatrix(&matrix);

			return node;
		};
		FbxSkeletalNode^ loadHierarchy(KFbxScene* pScene)
		{
			KFbxAnimEvaluator* evaluator = pScene->GetEvaluator();

			FbxSkeletalNode^ node = gcnew FbxSkeletalNode("", nullptr, Matrix::Identity);

			KFbxNode* lRootNode = pScene->GetRootNode();
			node->NodeName=gcnew String((char *) lRootNode->GetName());
			KFbxXMatrix matrix= evaluator->GetNodeGlobalTransform(lRootNode);
			node->TransformMatrix=this->FbxToMatrix(&matrix);
			for(int i = 0; i < lRootNode->GetChildCount(); i++)
			{
				FbxSkeletalNode^ subnode= loadOneNode(lRootNode->GetChild(i));
				//node->Children->Add(subnode);
				node->AddChildren(subnode);
			}

			return node;

		}
		FbxSkeletalNode^ loadModel(KFbxScene* pScene)
		{
			KFbxAnimEvaluator* evaluator = pScene->GetEvaluator();


			FbxSkeletalNode^ node = gcnew FbxSkeletalNode("", nullptr, Matrix::Identity);

			List<FbxMesh^>^ mirror=gcnew List<FbxMesh^>();
			KFbxNode* lRootNode = pScene->GetRootNode();
			node->NodeName=gcnew String((char *) lRootNode->GetName());
			KFbxXMatrix matrix= evaluator->GetNodeGlobalTransform(lRootNode);
			node->TransformMatrix=this->FbxToMatrix(&matrix);
			for(int i = 0; i < lRootNode->GetChildCount(); i++)
			{
				FbxSkeletalNode^ subnode= loadOneNode(lRootNode->GetChild(i),mirror);
				//node->Children->Add(subnode);
				node->AddChildren(subnode);
			}
			node->MeshesCache=mirror;

			return node;

		}


		FbxSkeletalNode^ cullFbxSkeletalNode(FbxSkeletalNode^ parent, FbxSkeletalNode^ root, StringCollection^ filter)
		{
			FbxSkeletalNode^ clone = gcnew FbxSkeletalNode();
			clone->SetOwner(root->GetOwner());
			clone->IsRoot = root->IsRoot;
			clone->NodeName = root->NodeName;
			clone->TransformMatrix = root->TransformMatrix;
			//
			for (int i = 0; i <root->Children->Count;i++)
			{
				FbxSkeletalNode^ node=cullFbxSkeletalNode(root, root->Children[i], filter);
				if (filter->Contains(node->NodeName) || node->Children->Count > 0)
				{
					//clone->Children->Add(node);
					clone->AddChildren(node);
				}
			}

			return clone;
		}

		FbxSkeletalNode^ cullSkeletalHierarchy(FbxSkeletalNode^ root, StringCollection^ filter)
		{

			return cullFbxSkeletalNode(nullptr, root, filter);
		}

		void exportSkeletalMap(FbxSkeletalNode^ root,StringCollection^ map)
		{
			map->Add(root->NodeName);

			for(int i=0;i<root->Children->Count;i++)
			{
				FbxSkeletalNode^ node= root->Children[i];
				exportSkeletalMap(node, map);
			}
		}
		FbxBoneInfo^ getCulledBoneRoot(StringCollection^ cullfilter, Dictionary<String^,Matrix>^ cullmap)
		{

			FbxBoneInfo^ bone=gcnew FbxBoneInfo ();
			FbxSkeletalNode^ root = cullSkeletalHierarchy(boneRoot, cullfilter);
			root->UpdateFrameMatrices(Matrix::Identity);
			//BoneContent^ boneroot=convertToBoneHierarchy(root);
			//includes hierarchy bones.
			StringCollection^ bonefilter=gcnew StringCollection();
			exportSkeletalMap(root, bonefilter);
			Dictionary<String^,Matrix>^ bonelink=gcnew Dictionary<String^,Matrix>();

			for(int i=0;i<cullfilter->Count;i++)
			{
				String^ linkname = cullfilter[i];
				if (!String::IsNullOrEmpty(linkname))
				{
					Matrix^ linkmatrix = cullmap[linkname];
					//boneroot->OpaqueData->Add(linkname, linkmatrix);
					bonelink->Add(linkname,*linkmatrix);
				}
			}
			bone->AllBoneNames=bonefilter;
			bone->LinkMatrixes=bonelink;
			bone->Root=root;

			return bone;
		}
		// for save bone hierarchy
		FbxBoneInfo^ getBoneRootInfo()
		{
			if(boneRoot==nullptr) return nullptr;
			//
			FbxBoneInfo^ bone=gcnew FbxBoneInfo ();
			FbxSkeletalNode^ root = boneRoot;
			//includes hierarchy bones.
			StringCollection^ bonefilter=gcnew StringCollection();
			exportSkeletalMap(root, bonefilter);
			bone->AllBoneNames=bonefilter;
			bone->Root=root;
			return bone;
		}
		//BoneContent^ getCulledBoneRoot(StringCollection^ cullfilter, Dictionary<String^,Matrix>^ cullmap)
		//{

		//	FbxSkeletalNode^ root = cullSkeletalHierarchy(boneRoot, cullfilter);
		//	BoneContent^ boneroot=convertToBoneHierarchy(root);
		//	//includes hierarchy bones.
		//	StringCollection^ bonefilter=gcnew StringCollection();
		//	exportSkeletalMap(root, bonefilter);
		//	//linkMatrix
		//	for(int i=0;i<cullfilter->Count;i++)
		//	{
		//		String^ linkname = cullfilter[i];
		//		if (!String::IsNullOrEmpty(linkname))
		//		{
		//			Matrix^ linkmatrix = cullmap[linkname];
		//			boneroot->OpaqueData->Add(linkname, linkmatrix);
		//		}
		//	}
		//	for(int i=0;i<animCol->Children->Count;i++)
		//	{
		//		FbxAnimationTake^ take=animCol->Children[i];

		//		AnimationContent^ xnaanim=gcnew AnimationContent ();
		//		xnaanim->Duration = TimeSpan::FromMilliseconds((take->PlayStop - take->PlayStart) * 1000.0 / take->FPS);
		//		for(int j=0;j<take->Children->Count;j++)
		//		{
		//			FbxAnimationPart ^part=take->Children[j];
		//			AnimationChannel^ xnacha = gcnew AnimationChannel();
		//			for(int k=0;k<part->Children->Count;k++)
		//			{
		//				FbxAnimationKey^ key=part->Children[k];
		//				if (key->Time >= take->PlayStart-100 && key->Time <= take->PlayStop)
		//				{
		//					int playkeytime = key->Time - take->PlayStart;
		//					TimeSpan xnatime = TimeSpan::FromMilliseconds(playkeytime * 1000 / take->FPS);
		//					AnimationKeyframe^ xnakey = gcnew AnimationKeyframe(xnatime, key->Matrix);
		//					xnacha->Add(xnakey);
		//				}
		//			}

		//			if (!String::IsNullOrEmpty(part->ObjectName) && bonefilter->Contains(part->ObjectName))
		//			{
		//				xnaanim->Channels->Add(part->ObjectName, xnacha);
		//			}
		//		}
		//		boneroot->Animations->Add(take->Name, xnaanim);
		//	}

		//	return boneroot;

		//}

		void ToDebug(String ^msg)
		{
			if(DEBUG)
				Console::WriteLine(msg);
		}
		String^ OutToString(KFbxLayerElement::EReferenceMode mode)
		{
			String^ temp="";
			switch(mode)
			{
			case KFbxLayerElement::eDIRECT:
				temp="eDIRECT";
				break;
			case KFbxLayerElement::eINDEX:
				temp="eINDEX";
				break;
			case KFbxLayerElement::eINDEX_TO_DIRECT:
				temp="eINDEX_TO_DIRECT";
				break;
			}
			return temp;
		}
		String^ OutToString(KFbxLayerElement::EMappingMode mode)
		{
			String^ temp="";
			switch(mode)
			{
			case KFbxLayerElement::eNONE:
				temp="eNONE";
				break;
			case KFbxLayerElement::eBY_CONTROL_POINT:
				temp="eBY_CONTROL_POINT";
				break;
			case KFbxLayerElement::eBY_POLYGON_VERTEX:
				temp="eBY_POLYGON_VERTEX";
				break;
			case KFbxLayerElement::eBY_POLYGON:
				temp="eBY_POLYGON";
				break;
			case KFbxLayerElement::eBY_EDGE:
				temp="eBY_EDGE";
				break;
			case KFbxLayerElement::eALL_SAME:
				temp="eALL_SAME";
				break;
			}
			return temp;
		};

		String^ getDefaultTexture()
		{
			String^ pTex=nullptr;
			String ^textfilename=gcnew String("default.jpg");
			String ^uripath = textfilename;
			if(!File::Exists(uripath))
			{   //maybe local file path
				textfilename=Path::GetFileName(uripath);
				uripath=Path::Combine( initialDirectory , textfilename);
			}
			//AppDomain.CurrentDomain.BaseDirectory
			if(!File::Exists(uripath))
			{   //maybe local file path
				textfilename=Path::GetFileName(uripath);
				uripath=Path::Combine( AppDomain::CurrentDomain->BaseDirectory , textfilename);
			}
			if(File::Exists(uripath))
			{
				ToDebug("Default Textures:"+uripath);

				//material->Texture = gcnew String(uripath);
				pTex = gcnew String(uripath);

			}
			else
			{
				ToDebug(gcnew String("Cannot found default texture:")+textfilename);
			}
			return pTex;
		}

		FbxMaterial^ GetMaterial(List<FbxMaterial^>^ matDirect,  int direct)
		{
			FbxMaterial^ basic=nullptr;
			int index=direct;
			bool texture=false;

			if(direct>=0 && direct<matDirect->Count)
			{
				basic=matDirect[index];
				if(!String::IsNullOrEmpty( basic->Texture))
				{
					texture=true;
				}
			}

			if(!texture&& basic!=nullptr)
			{
				//basic->VertexColorEnabled=true;
				//ToDebug("Use Default Texture by "+direct +" > " + index);
				basic->Texture=getDefaultTexture();
			}
			else if(texture)
			{
				//ToDebug("Use Texture:"+basic->Texture->Filename +" by "+direct +" > " + index);

			}
			return basic;
		};

		FbxMesh^ parseMesh(KFbxNode* pNode, KFbxMesh *pMesh)
		{

			String ^meshname=gcnew String(pNode->GetName());

			FbxMeshBuilder^ builder = FbxMeshBuilder::StartMesh(meshname);
			List<FbxMaterial^>^ materialList=gcnew List<FbxMaterial^>();


			KFbxPropertyDouble3 lKFbxDouble3;
			KFbxPropertyDouble1 lKFbxDouble1;
			KFbxColor theColor;
			String^ mname=nullptr;
			FbxMaterial^ material;
			int lMaterialCount = pNode->GetMaterialCount(); //pNode->GetSrcObjectCount( KFbxSurfaceMaterial::ClassId );

			for (int lCount = 0; lCount < lMaterialCount; lCount ++)
			{
				KFbxSurfaceMaterial *lMaterial = pNode->GetMaterial(lCount); //(KFbxSurfaceMaterial*)pNode->GetSrcObject(KFbxSurfaceMaterial::ClassId, lCount);

				material=gcnew FbxMaterial();

				mname= gcnew String(lMaterial->GetName());
				if(lMaterial->GetClassId().Is(KFbxSurfaceLambert::ClassId) )
				{
					// We found a Lambert material. Display its properties.
					// Display the Ambient Color
					lKFbxDouble3=((KFbxSurfaceLambert *)lMaterial)->GetAmbientColor();
					theColor.Set(lKFbxDouble3.Get()[0], lKFbxDouble3.Get()[1], lKFbxDouble3.Get()[2]);
					//DisplayColor("            Ambient: ", theColor);


					// Display the Diffuse Color
					lKFbxDouble3 =((KFbxSurfaceLambert *)lMaterial)->GetDiffuseColor();
					theColor.Set(lKFbxDouble3.Get()[0], lKFbxDouble3.Get()[1], lKFbxDouble3.Get()[2]);
					//DisplayColor("            Diffuse: ", theColor);
					material->DiffuseColor= Rgb2Vector3(theColor);

					// Display the Emissive
					lKFbxDouble3 =((KFbxSurfaceLambert *)lMaterial)->GetEmissiveColor();
					theColor.Set(lKFbxDouble3.Get()[0], lKFbxDouble3.Get()[1], lKFbxDouble3.Get()[2]);
					//DisplayColor("            Emissive: ", theColor);
					material->EmissiveColor= Rgb2Vector3(theColor);

					// Display the Opacity
					lKFbxDouble1 =((KFbxSurfaceLambert *)lMaterial)->GetTransparencyFactor();
					//DisplayDouble("            Opacity: ", 1.0-lKFbxDouble1.Get());
					material->Alpha= (float)(1.0-lKFbxDouble1.Get());

				}
				else if (lMaterial->GetClassId().Is(KFbxSurfacePhong::ClassId))
				{
					// We found a Phong material.  Display its properties.

					// Display the Ambient Color
					lKFbxDouble3 =((KFbxSurfacePhong *) lMaterial)->GetAmbientColor();
					theColor.Set(lKFbxDouble3.Get()[0], lKFbxDouble3.Get()[1], lKFbxDouble3.Get()[2]);
					//DisplayColor("            Ambient: ", theColor);

					// Display the Diffuse Color
					lKFbxDouble3 =((KFbxSurfacePhong *) lMaterial)->GetDiffuseColor();
					theColor.Set(lKFbxDouble3.Get()[0], lKFbxDouble3.Get()[1], lKFbxDouble3.Get()[2]);
					//DisplayColor("            Diffuse: ", theColor);
					material->DiffuseColor= Rgb2Vector3(theColor);

					// Display the Specular Color (unique to Phong materials)
					lKFbxDouble3 =((KFbxSurfacePhong *) lMaterial)->GetSpecularColor();
					theColor.Set(lKFbxDouble3.Get()[0], lKFbxDouble3.Get()[1], lKFbxDouble3.Get()[2]);
					//DisplayColor("            Specular: ", theColor);
					material->SpecularColor= Rgb2Vector3(theColor);

					// Display the Emissive Color
					lKFbxDouble3 =((KFbxSurfacePhong *) lMaterial)->GetEmissiveColor();
					theColor.Set(lKFbxDouble3.Get()[0], lKFbxDouble3.Get()[1], lKFbxDouble3.Get()[2]);
					//DisplayColor("            Emissive: ", theColor);
					material->EmissiveColor= Rgb2Vector3(theColor);

					//Opacity is Transparency factor now
					lKFbxDouble1 =((KFbxSurfacePhong *) lMaterial)->GetTransparencyFactor();
					//DisplayDouble("            Opacity: ", 1.0-lKFbxDouble1.Get());


					// Display the Shininess
					lKFbxDouble1 =((KFbxSurfacePhong *) lMaterial)->GetShininess();
					//DisplayDouble("            Shininess: ", lKFbxDouble1.Get());
					material->SpecularPower=(float)(lKFbxDouble1.Get());

					// Display the Reflectivity
					lKFbxDouble1 =((KFbxSurfacePhong *) lMaterial)->GetReflectionColor();
					//DisplayDouble("            Reflectivity: ", lKFbxDouble1.Get());	
				}

				//textures
				KFbxProperty lPropDiffuse = lMaterial->FindProperty( KFbxSurfaceMaterial::sDiffuse );
				if( lPropDiffuse.IsValid() )
				{

					int lLayeredTextureCount = lPropDiffuse.GetSrcObjectCount(KFbxLayeredTexture::ClassId);
					if(lLayeredTextureCount > 0)
					{
						for(int j=0; j<lLayeredTextureCount; ++j)
						{
							KFbxLayeredTexture *lLayeredTexture = (KFbxLayeredTexture*)lPropDiffuse.GetSrcObject(KFbxLayeredTexture::ClassId, 0);
							int lNbTextures = lLayeredTexture->GetSrcObjectCount(KFbxTexture::ClassId);
							for(int k =0; k<lNbTextures; ++k)
							{
								//let's say I want to blendmode of that layered texture:
								KFbxLayeredTexture::EBlendMode lBlendMode;
								lLayeredTexture->GetTextureBlendMode(k, lBlendMode);
								KFbxTexture* lTexture = KFbxCast <KFbxTexture> (lLayeredTexture->GetSrcObject(KFbxTexture::ClassId,j));
								//KString lTemp = lTexture->GetRelativeFileName();
								if(lTexture)
								{ 
									switch(j)
									{
									case 0:
										KFbxFileTexture *lFileTexture = KFbxCast<KFbxFileTexture>(lTexture);

										material->Texture=  gcnew String( lFileTexture->GetFileName());
										break;

									}
								}
							}
						}
					}
					else
					{
						int lNbTextures = lPropDiffuse.GetSrcObjectCount(KFbxTexture::ClassId);
						for(int j =0; j<lNbTextures; ++j)
						{
							KFbxTexture* lTexture = KFbxCast <KFbxTexture> (lPropDiffuse.GetSrcObject(KFbxTexture::ClassId,j));
							//KString lTemp = lTexture->GetRelativeFileName();
							if(lTexture)
							{
								KFbxFileTexture *lFileTexture = KFbxCast<KFbxFileTexture>(lTexture);

								material->Texture=  gcnew String(lFileTexture->GetFileName());
							}
						}
					}
				}
				//

				materialList->Add(material);

			}

			// texture
			//   int lMaterialIndex;
			//int lTextureIndex;
			//KFbxProperty lProperty;    
			//int lNbMat = pNode->GetSrcObjectCount(KFbxSurfaceMaterial::ClassId);
			//for (lMaterialIndex = 0; lMaterialIndex < lNbMat; lMaterialIndex++)
			//{
			//	KFbxSurfaceMaterial *lMaterial = (KFbxSurfaceMaterial *)pNode->GetSrcObject(KFbxSurfaceMaterial::ClassId, lMaterialIndex);
			//                   lProperty = lMaterial->FindProperty(KFbxSurfaceMaterial::sDiffuse);
			//	if( lProperty.IsValid() )
			//	{
			//		int lLayeredTextureCount = lProperty.GetSrcObjectCount(KFbxLayeredTexture::ClassId);
			//		if(lLayeredTextureCount > 0)
			//		{
			//			Console::WriteLine("Note:Layer textures is not supported!");
			//			continue;
			//		}
			//		int lNbTextures = lProperty.GetSrcObjectCount(KFbxTexture::ClassId);
			//		for(int j =0; j<lNbTextures; ++j)
			//		{

			//			KFbxTexture* lTexture = KFbxCast <KFbxTexture> (lProperty.GetSrcObject(KFbxTexture::ClassId,j));
			//			if(lTexture)
			//			{
			//				//display connectMareial header only at the first time
			//				if(lMaterialIndex>= materialList->Count)
			//				{
			//					Console::WriteLine("Note:textures lMaterialIndex exceeds the materailList!");
			//			        continue;
			//				}
			//				FbxMaterial^ mat= materialList[lMaterialIndex];
			//				mat->Texture=   gcnew String( lTexture->GetFileName());
			//			}
			//		}

			//	}
			//   }


			///
			StringCollection^ cullfilter = gcnew StringCollection();
			Dictionary<String^,Matrix>^ cullmap=gcnew Dictionary<String^,Matrix>();
			int pointcount=pMesh->GetControlPointsCount();
			List<FbxBoneWeightCollection^>^ lstweight = gcnew List<FbxBoneWeightCollection^>();
			for (int i = 0; i < pointcount; i++)
			{
				lstweight->Add(gcnew FbxBoneWeightCollection());
			}
			//skin
			//String^ storyname=this->defaultStoryName;
			int lSkinCount=0;
			int lClusterCount=0;
			KFbxCluster* lCluster;
			lSkinCount=pMesh->GetDeformerCount(KFbxDeformer::eSKIN);
			for(int i=0; i!=lSkinCount; ++i)
			{
				lClusterCount = ((KFbxSkin *) pMesh->GetDeformer(i, KFbxDeformer::eSKIN))->GetClusterCount();
				for (int j = 0; j != lClusterCount; ++j)
				{
					lCluster=((KFbxSkin *) pMesh->GetDeformer(i, KFbxDeformer::eSKIN))->GetCluster(j);


					KFbxXMatrix ciMatrix,riMatrix;
					lCluster->GetTransformMatrix(riMatrix);
					lCluster->GetTransformLinkMatrix(ciMatrix);
					//
					riMatrix*=GetGeometry(pNode);
					//
					riMatrix=ciMatrix.Inverse() * riMatrix;

					//link->LinkMatrix = FbxToMatrix(&riMatrix);

					String^ linkname="";
					if(lCluster->GetLink() != NULL)
					{
						//DisplayString("        Name: ", (char *) lCluster->GetLink()->GetName());
						linkname=gcnew String((char *) lCluster->GetLink()->GetName());
					}
					cullfilter->Add(linkname);
					cullmap->Add(linkname,FbxToMatrix(&riMatrix));

					int  lIndexCount = lCluster->GetControlPointIndicesCount();
					int* lIndices = lCluster->GetControlPointIndices();
					double* lWeights = lCluster->GetControlPointWeights();

					//link->VertexIndices = gcnew Int32Collection(lIndexCount);
					//link->Weights = gcnew DoubleCollection(lIndexCount);

					for(int k = 0; k < lIndexCount; k++)
					{
						//link->VertexIndices->Add(lIndices[k]);
						//link->Weights->Add(lWeights[k]);
						FbxBoneWeight^ bw=gcnew FbxBoneWeight(linkname,(float)lWeights[k]);
						lstweight[lIndices[k]]->Add(bw) ;
					}
				}
			}

			//check normal
			pMesh->ComputeVertexNormals();

			int laycount=pMesh->GetLayerCount();
			KFbxLayerElementMaterial* leMat=NULL;
			if(laycount>0)
			{
				leMat = pMesh->GetLayer(0)->GetMaterials();
			}
			int lIndexArrayCount = 0;
			if(leMat)
			{
				lIndexArrayCount=leMat->GetIndexArray().GetCount(); 
			}

			Dictionary<int, int>^ pointmap = gcnew Dictionary<int, int>();
			KFbxVector4* points = pMesh->GetControlPoints();
			for(int i=0;i<pointcount;i++)
			{
				//mesh->Positions->Add(Point3D(points[i][0],points[i][1],points[i][2])); 
				TempUnitScale(points[i]);
				int pointindex= builder->CreatePosition((float)points[i][0],(float)points[i][1],(float)points[i][2]);
				pointmap->Add(i,pointindex);
			}
			//polygons and textures only for 0 layers
			//int point3[3];
			int vertexId = 0;

			KFbxLayerElementUV* leUV;
			KFbxLayerElementVertexColor* leVtxc=NULL ;
			if(laycount>0)
			{
				leVtxc= pMesh->GetLayer(0)->GetVertexColors();
			}
			//KFbxLayerElementTexture * leTex=pMesh->GetLayer(0)->GetTextures( KFbxLayerElement:: eDIFFUSE_TEXTURES);
			//leTex->gett
			int polycount=pMesh->GetPolygonCount();
			for (int i = 0; i <polycount; i++)
			{
				int polysize=pMesh->GetPolygonSize(i);
				//
				if( leMat && leMat->GetMappingMode()== KFbxLayerElement::eALL_SAME)
				{
					if (leMat->GetReferenceMode() == KFbxLayerElement::eINDEX ||
						leMat->GetReferenceMode() == KFbxLayerElement::eINDEX_TO_DIRECT)
					{
						if(i<lIndexArrayCount)
						{
							int iMat=  leMat->GetIndexArray().GetAt(i);
							material=GetMaterial( materialList,iMat);
							if(material!=nullptr)builder->SetMaterial(material);
						}
					}

				}

				for(int j=0;j< polysize;j++)
				{
					int lControlPointIndex = pMesh->GetPolygonVertex(i, j);
					KFbxVector4 lNormal;
					pMesh->GetPolygonVertexNormal(i,j,lNormal);
					//
					if( leMat && leMat->GetMappingMode()== KFbxLayerElement::eBY_POLYGON)
					{
						if (leMat->GetReferenceMode() == KFbxLayerElement::eINDEX ||
							leMat->GetReferenceMode() == KFbxLayerElement::eINDEX_TO_DIRECT)
						{
							if(i<lIndexArrayCount)
							{
								int iMat=  leMat->GetIndexArray().GetAt(i);
								material=GetMaterial( materialList,iMat);
								if(material!=nullptr)builder->SetMaterial(material);
							}
						}

					}
					if (leVtxc)
					{
						KFbxColor kc=KFbxColor(0,0,0);
						switch (leVtxc->GetMappingMode())
						{
						case KFbxLayerElement::eBY_CONTROL_POINT:
							switch (leVtxc->GetReferenceMode())
							{
							case KFbxLayerElement::eDIRECT:
								//DisplayColor(header, leVtxc->GetDirectArray().GetAt(lControlPointIndex));
								kc=leVtxc->GetDirectArray().GetAt(lControlPointIndex);
								break;
							case KFbxLayerElement::eINDEX_TO_DIRECT:
								{
									int id = leVtxc->GetIndexArray().GetAt(lControlPointIndex);
									//DisplayColor(header, leVtxc->GetDirectArray().GetAt(id));
									kc=leVtxc->GetDirectArray().GetAt(id);
								}
								break;
							default:
								break; // other reference modes not shown here!
							}
							break;

						case KFbxLayerElement::eBY_POLYGON_VERTEX:
							{
								switch (leVtxc->GetReferenceMode())
								{
								case KFbxLayerElement::eDIRECT:
									//DisplayColor(header, leVtxc->GetDirectArray().GetAt(vertexId));
									kc= leVtxc->GetDirectArray().GetAt(vertexId);
									break;
								case KFbxLayerElement::eINDEX_TO_DIRECT:
									{
										int id = leVtxc->GetIndexArray().GetAt(vertexId);
										//DisplayColor(header, leVtxc->GetDirectArray().GetAt(id));
										kc=leVtxc->GetDirectArray().GetAt(id);
									}
									break;
								default:
									break; // other reference modes not shown here!
								}
							}
							break;

						case KFbxLayerElement::eBY_POLYGON: // doesn't make much sense for UVs
						case KFbxLayerElement::eALL_SAME:   // doesn't make much sense for UVs
						case KFbxLayerElement::eNONE:       // doesn't make much sense for UVs
							break;
						}
						//
						Vector4 v4= Rgb2Vector4(kc);
						builder->SetVertexColor(v4);
					}

					for(int k=0;k<laycount;k++)
					{
						leUV = pMesh->GetLayer(k)->GetUVs();

						if(leUV)
						{
							//mesh->TextureCoordinates->Add(Point(,));
							KFbxVector2 v2=KFbxVector2(0,0);

							switch (leUV->GetMappingMode())
							{
							case KFbxLayerElement::eBY_CONTROL_POINT:
								switch (leUV->GetReferenceMode())
								{
								case KFbxLayerElement::eDIRECT:
									//Display2DVector(header, leUV->GetDirectArray().GetAt(lControlPointIndex));
									v2= leUV->GetDirectArray().GetAt(lControlPointIndex);
									break;
								case KFbxLayerElement::eINDEX_TO_DIRECT:
									{
										int id = leUV->GetIndexArray().GetAt(lControlPointIndex);
										//Display2DVector(header, leUV->GetDirectArray().GetAt(id));
										v2=leUV->GetDirectArray().GetAt(id);
									}
									break;
								default:
									break; // other reference modes not shown here!
								}
								break;

							case KFbxLayerElement::eBY_POLYGON_VERTEX:
								{
									int lTextureUVIndex = pMesh->GetTextureUVIndex(i, j);
									switch (leUV->GetReferenceMode())
									{
									case KFbxLayerElement::eDIRECT:
										//Display2DVector(header, leUV->GetDirectArray().GetAt(lTextureUVIndex));
										v2=leUV->GetDirectArray().GetAt(lTextureUVIndex);
										break;
									case KFbxLayerElement::eINDEX_TO_DIRECT:
										{
											//int id = leUV->GetIndexArray().GetAt(lTextureUVIndex);
											//Display2DVector(header, leUV->GetDirectArray().GetAt(id));
											//v2=leUV->GetDirectArray().GetAt(id);
											v2=leUV->GetDirectArray().GetAt(lTextureUVIndex);
										}
										break;
									default:
										break; // other reference modes not shown here!
									}
								}
								break;

							case KFbxLayerElement::eBY_POLYGON: // doesn't make much sense for UVs
							case KFbxLayerElement::eALL_SAME:   // doesn't make much sense for UVs
							case KFbxLayerElement::eNONE:       // doesn't make much sense for UVs
								break;
							}
							//mesh->TextureCoordinates->Add(Point(v2[0],v2[1]));
							Vector2^ vect = gcnew Vector2();
							vect->X = (float)v2[0];
							vect->Y = 1-(float)v2[1];

							//ToDebug("uv:"+v2[0]+ ","+ v2[1]);
							//builder->SetVertexChannelData(texCoordId, vect);
							builder->SetVertexTextureCords(k,*vect);

						}
					}
					//weight
					if (lSkinCount>0)
					{
						//builder->SetVertexChannelData(weightId, lstweight[lControlPointIndex]);
						builder->SetVertexBoneWeights(lstweight[lControlPointIndex]);
					}
					//normal
					Vector3^ vect3=gcnew Vector3();
					vect3->X=(float) lNormal[0];
					vect3->Y=(float) lNormal[1];
					vect3->Z=(float) lNormal[2];
					builder->SetVertexNormal(*vect3);


					//
					builder->AddTriangleVertex(pointmap->default[lControlPointIndex]);

					vertexId++;

				}//polysize
			}
			//normals

			FbxMesh^ meshContent = builder->FinishMesh();

			if(meshContent!=nullptr)
			{
				if(this->bSwapWindingOrder)
				{
					FbxMeshBuilder::SwapWindingOrder(meshContent);
				}

				if (meshContent->HasGeometry )
				{
					if( lSkinCount>0)
					{
						meshContent->BoneInfo=getCulledBoneRoot(cullfilter,cullmap);
						// Add the mesh to the model
					}
				}


				KFbxAnimEvaluator* evaluator = 
					pNode->GetScene()->GetEvaluator();

				KFbxXMatrix matrix= evaluator->GetNodeGlobalTransform(pNode);
				meshContent->BoneTransform=FbxToMatrix(&matrix);
			}


			return meshContent;

		};
		//void dealNode(KFbxNode* pNode, int hierarchy, NodeContent ^ pgroup)
		//{
		//	KFbxNodeAttribute::EAttributeType lAttributeType;
		//	int i;

		//	if(pNode->GetNodeAttribute() == NULL)
		//	{
		//		//printf("NULL Node Attribute\n\n");
		//	}
		//	else
		//	{
		//		lAttributeType = (pNode->GetNodeAttribute()->GetAttributeType());

		//		switch (lAttributeType)
		//		{
		//		case KFbxNodeAttribute::eMARKER:  
		//			//DisplayMarker(pNode);
		//			break;

		//		case KFbxNodeAttribute::eSKELETON:  
		//			//DisplaySkeleton(pNode);
		//			break;

		//		case KFbxNodeAttribute::eMESH:      
		//			{
		//				//DisplayMesh(pNode);
		//				KFbxMesh * origMesh=(KFbxMesh*)pNode->GetNodeAttribute();
		//				KFbxMesh * pMesh= globalConverter->TriangulateMesh(origMesh);
		//				NodeContent ^geo=parseMesh(pNode,pMesh);
		//				pgroup->Children->Add(geo);
		//			}
		//			break;
		//		case KFbxNodeAttribute::eNURB:      
		//			{
		//				KFbxNurb  * origNurb=(KFbxNurb *)pNode->GetNodeAttribute();
		//				KFbxMesh * pMesh= globalConverter->TriangulateNurb (origNurb);
		//				NodeContent ^geo=parseMesh(pNode,pMesh);
		//				pgroup->Children->Add(geo);
		//			}
		//			break;

		//		case KFbxNodeAttribute::ePATCH:     
		//			{
		//				KFbxPatch  * origPatch=(KFbxPatch *)pNode->GetNodeAttribute();
		//				KFbxMesh * pMesh= globalConverter->TriangulatePatch (origPatch);
		//				NodeContent ^geo=parseMesh(pNode,pMesh);
		//				pgroup->Children->Add(geo);
		//			}
		//			break;

		//		case KFbxNodeAttribute::eCAMERA:    
		//			//DisplayCamera(pNode);
		//			//dealCamera((KFbxCamera*)pNode->GetNodeAttribute());
		//			break;

		//		case KFbxNodeAttribute::eLIGHT:     
		//			//DisplayLight(pNode);
		//			//Light^ light=parseLight((KFbxLight*)pNode->GetNodeAttribute());
		//			//if(light!=nullptr)
		//			//{
		//			//	pgroup->Children->Add(light);
		//			//}
		//			break;
		//		}   
		//	}

		//
		//	int childcount=pNode->GetChildCount();
		//	for(i = 0; i < childcount; i++)
		//	{
		//		NodeContent^ subgroup = gcnew NodeContent();
		//		dealNode(pNode->GetChild(i),hierarchy+1,subgroup);
		//		if(subgroup->Children->Count>0)
		//		{
		//			pgroup->Children->Add(subgroup);
		//		}
		//	}

		//	//
		//	KFbxXMatrix matrix= pNode-> GetGlobalFromDefaultTake();
		//	KFbxNode* pParent=pNode->GetParent();
		//	if(pParent)
		//	{
		//		KFbxXMatrix matrix2= pParent-> GetGlobalFromDefaultTake();
		//		matrix=matrix2.Inverse()* matrix;
		//	}
		//	pgroup->Transform=FbxToMatrix(&matrix);


		//};



		void parse(KFbxScene* lScene)
		{
			//NodeContent^ node= gcnew NodeContent();
			//meta info
			KFbxDocumentInfo* sceneInfo = lScene->GetSceneInfo();
			if (sceneInfo)
			{
				/*printf("    Title: %s\n", sceneInfo->mTitle.GetBuffer());
				printf("    Subject: %s\n", sceneInfo->mSubject.GetBuffer());
				printf("    Author: %s\n", sceneInfo->mAuthor.GetBuffer());
				printf("    Keywords: %s\n", sceneInfo->mKeywords.GetBuffer());
				printf("    Revision: %s\n", sceneInfo->mRevision.GetBuffer());
				printf("    Comment: %s\n", sceneInfo->mComment.GetBuffer());*/
			}

			//
			KFbxGlobalLightSettings *pGlobalLightSettings=& lScene->GlobalLightSettings();
			//ignore global light
			KFbxGlobalCameraSettings *pGlobalCameraSettings=& lScene->GlobalCameraSettings();
			//pGlobalCameraSettings->GetDefaultCamera();
			/*this->dealCamera(&pGlobalCameraSettings->GetCameraProducerPerspective());
			this->dealCamera(&pGlobalCameraSettings->GetCameraProducerTop());
			this->dealCamera(&pGlobalCameraSettings->GetCameraProducerBottom());
			this->dealCamera(&pGlobalCameraSettings->GetCameraProducerFront());
			this->dealCamera(&pGlobalCameraSettings->GetCameraProducerBack());
			this->dealCamera(&pGlobalCameraSettings->GetCameraProducerLeft());
			this->dealCamera(&pGlobalCameraSettings->GetCameraProducerRight());*/
			//KFbxGlobalTimeSettings *pGlobalTimeSettings=&lScene->GetGlobalTimeSettings();
			KFbxGlobalSettings * pGlobalSettings=& lScene->GetGlobalSettings();
			KFbxAxisSystem pSystem= pGlobalSettings->GetAxisSystem();


			KFbxAxisSystem::eUpVector up= KFbxAxisSystem::eUpVector::ZAxis;
			if(!zUP)
			{
				up=KFbxAxisSystem::eUpVector::YAxis;
			}
			KFbxAxisSystem::eCoorSystem coord= pSystem.GetCoorSystem();
			if(this->bLeftHand)
			{
				coord=KFbxAxisSystem::eCoorSystem::LeftHanded;
			}
			else
			{
				coord=KFbxAxisSystem::eCoorSystem::RightHanded;
			}

			//KFbxAxisSystem zSystem(up, KFbxAxisSystem::eFrontVector::ParityOdd,pSystem.GetCoorSystem());
			//KFbxAxisSystem zSystem(up, KFbxAxisSystem::eFrontVector::ParityOdd,KFbxAxisSystem::eCoorSystem::RightHanded);
			KFbxAxisSystem zSystem(up, KFbxAxisSystem::eFrontVector::ParityOdd,coord);
			zSystem.ConvertScene(lScene);
			//unit scale

			/* KFbxSystemUnit kUnitScaler(this->unitScale);

			KFbxSystemUnit::KFbxUnitConversionOptions ko;
			ko.mConvertLightIntensity=false;
			ko.mConvertRrsNodes=false;
			kUnitScaler.ConvertScene(lScene,ko);*/

			//generic information
			//FbxSkeletalNode^ 
			boneRoot = loadHierarchy( lScene);
			//FbxAnimationCollection^
			animCol = gcnew FbxAnimationCollection();

			rootmodel=loadModel(lScene);
			//hierarchy
			//KFbxNode* lRootNode = lScene->GetRootNode();
			//if(lRootNode)
			//{
			//	int childcount=lRootNode->GetChildCount();
			//	for(int i = 0; i < childcount; i++)
			//	{
			//		//lRootNode->
			//		dealNode(lRootNode->GetChild(i), 0, node);
			//	}
			//	KFbxXMatrix matrix= lRootNode-> GetGlobalFromDefaultTake();
			//	node->Transform=FbxToMatrix(&matrix);

			//}
			//content
			//pose
			//animation



			//return node;
		};

		void tryParse()
		{
			if(!this->Ready)
			{
				KFbxScene* lScene = NULL;
				bool lResult;

				// Prepare the FBX SDK.
				//::InitializeSdkObjects(lSdkManager, lScene);

				lScene = KFbxScene::Create(lSdkManager,"");

				globalConverter=new KFbxGeometryConverter(lSdkManager);

				//lSdkManager=KFbxSdkManager::CreateKFbxSdkManager();
				//lScene= KFbxScene::Create(lSdkManager,"");
				//load fbx model
				char* str2 = (char*)(void*)Marshal::StringToHGlobalAnsi(this->inputURL);
				lResult = ::LoadScene(lSdkManager, lScene, str2);
				Marshal::FreeHGlobal((IntPtr)str2);
				//parse
				//						this->rootNode=parse(lScene);
				parse(lScene);

				delete globalConverter;
				//free memory
				//::DestroySdkObjects(lSdkManager);
				//lSdkManager->DestroyKFbxSdkManager();
				this->Ready = true;
			}

		};
	public:
		FbxParser(String^ uri)
		{
			//this(uri,true);
			this->DEBUG=false;
			this->Ready=false;
			this->DefaultBoneName="DefaultBone";
			this->inputURL=uri;
			this->initialDirectory=Path::GetDirectoryName(this->inputURL);
			this->zUP=true;
			this->unitScale=1.0;
			this->bSwapWindingOrder=true;
			this->bLeftHand=false;
		}
		///z is up, otherwise y is up.
		FbxParser(String^ uri, bool zUp)
		{
			this->DEBUG=false;
			this->Ready=false;
			this->DefaultBoneName="DefaultBone";
			this->inputURL=uri;
			this->initialDirectory=Path::GetDirectoryName(this->inputURL);
			this->zUP=zUp;
			this->unitScale=1.0;
			this->bSwapWindingOrder=true;
			this->bLeftHand=false;
		};
		///The equivalent number of centimeters in the new system unit. eg For an inch unit, use a scale factor of 2.54
		FbxParser(String^ uri, bool zUp, double unitScale)
		{
			this->DEBUG=false;
			this->Ready=false;
			this->DefaultBoneName="DefaultBone";
			this->inputURL=uri;
			this->initialDirectory=Path::GetDirectoryName(this->inputURL);
			this->zUP=zUp;
			this->unitScale=unitScale;
			this->bSwapWindingOrder=true;
			this->bLeftHand=false;
		};


		void SetDebugMode(bool isDebug)
		{
			DEBUG=isDebug;

		};

		void SetSwapWindingOrder(bool bSwap)
		{
			this->bSwapWindingOrder=bSwap;
		}
		void SetCoordinate(bool bLeftHand)
		{
			this->bLeftHand=bLeftHand;
		}

		/*virtual NodeContent^ GetNodeContent() 
		{
		tryParse();
		return this->rootNode;
		};*/

		/*virtual X3DModel^ GetNodeContent() 
		{
		tryParse();
		return this->_model;
		};*/
		virtual FbxSkeletalNode^ GetModel() 
		{
			tryParse();
			return this->rootmodel;

		};
		virtual FbxSkeletalNode^ GetSkeletalHierarchy() 
		{
			tryParse();
			return this->boneRoot;

		};
		virtual FbxAnimationCollection^ GetAnimation() 
		{
			tryParse();
			return this->animCol;

		};

	};
}
