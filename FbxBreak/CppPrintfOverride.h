#pragma once

#include "FbxBreak.h"

#define printf(x, ...) managed_printf(x, __VA_ARGS__)

void managed_printf(System::String^ baseString, [System::ParamArray] array<const char*>^ args)
{
	FbxBreak::FbxModelBreaker::globalMessages->Add(baseString);

	for(int i = 0;i<args->Length;i++)
	{
		FbxBreak::FbxModelBreaker::globalMessages->Add(gcnew System::String(args[i]));
	}
}