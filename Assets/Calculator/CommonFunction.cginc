
//This function return 4-coordinate
//----------------------------------------
//sites are sitenumber x sitenumber x sitenumber x sitenumber x dir (max is 2^26 - 1)
//dir is always forward, so site (0,0,0,0)-d is (0,0,0,0,d)
//there are 2^(D-1) plaquettes
//----------------------------------------
inline int GetGvalueG4096_4D(RWTexture2D<int2> conf, uint2 site, uint dir)
{
    if (0 == dir)
    {
        return conf[site].r >> 12;
    }
    else if (1 == dir)
    {
        return conf[site].r & 4095;
    }
    else if (2 == dir)
    {
        return conf[site].g >> 12;
    }
    return conf[site].g & 4095;
}

//-------------------------------------
//L site index to uint2 site index
//-------------------------------------
inline uint2 GetSiteIndexG4096_4D(uint L, uint isiteshift, uint siteMask)
{
    return uint2((((L >> (3 * (isiteshift + 1))) & siteMask) << isiteshift)
                + ((L >> (2 * (isiteshift + 1))) & siteMask),
                 (((L >> (1 * (isiteshift + 1))) & siteMask) << isiteshift)
                + (L & siteMask)
                );
}

//-------------------------------------
//Get Plaquette
//-------------------------------------
//Forward
//     c
//   +-<-+
// d |   | b ^
//   +->-+
//     a
// b_dir = boundDir, p_dir = plaquette dir
// a = [site][b_dir]
// b = [site+b_dir][p_dir]
// c = [site+p_dir][b_dir]^-1
// d = [site][p_dir]^-1
// Uabcd
// return uint3(b, c, d)
inline uint3 GetForwardPlaquette(uint4 fs, uint plaqutte, uint boundDir, uint L, uint iSiteShift, uint mask2, RWTexture2D<int2> conf, uint iM, uint iN)
{
    uint Lprime1 = (fs[plaqutte] + L);
    uint Lprime2 = (fs[boundDir] + L);

    uint gb1 = GetGvalueG4096_4D(conf, GetSiteIndexG4096_4D(Lprime2, iSiteShift, mask2), plaqutte);
    uint gc1 = GetGvalueG4096_4D(conf, GetSiteIndexG4096_4D(Lprime1, iSiteShift, mask2), boundDir);
    uint gd1 = GetGvalueG4096_4D(conf, GetSiteIndexG4096_4D(L, iSiteShift, mask2), plaqutte);
    return uint3(gb1, iM + iN - 2 - gc1, iM + iN - 2 - gd1);
}

//Backward
//     a
//   +-<-+
// d |   | b ^
//   +->-+
//     c
// b_dir = boundDir, p_dir = plaquette dir
// a = [site][b_dir]^-1
// b = [site+b_dir-p_dir][p_dir]
// c = [site-p_dir][b_dir]
// d = [site-p_dir][p_dir]^-1
// Ucbad
// return uint3(c, b, d)
inline uint3 GetBackwardPlaquette(uint4 fs, uint4 bs, uint plaqutte, uint boundDir, uint L, uint iSiteShift, uint mask2, RWTexture2D<int2> conf, uint iM, uint iN)
{
    uint Lprime3 = (bs[plaqutte] + L);
    uint Lprime4 = (bs[plaqutte] + fs[boundDir] + L);
    uint gb2 = GetGvalueG4096_4D(Configuration, GetSiteIndexG4096_4D(Lprime4, iSiteShift, mask2), plaqutte);
    uint gc2 = GetGvalueG4096_4D(Configuration, GetSiteIndexG4096_4D(Lprime3, iSiteShift, mask2), boundDir);
    uint gd2 = GetGvalueG4096_4D(Configuration, GetSiteIndexG4096_4D(Lprime3, iSiteShift, mask2), plaqutte);
    return uint3(gc2, gb2, iM + iN - 2 - gd2);
}

//-------------------------------------
//Get Plaquette
//-------------------------------------
inline float2 CalcEnergy(uint plaqutte, uint2 oldA, uint2 newA, uint3 plaq, RWTexture2D<int> mt, RWTexture2D<float> etable)
{
    uint resultGIndexOld = ((plaqutte & 1) == 1) ?
          mt[uint2(mt[uint2(oldA.x, plaq.x)], mt[uint2(plaq.y, plaq.z)])]
        : mt[uint2(mt[uint2(plaq.x, plaq.y)], mt[uint2(oldA.y, plaq.z)])];

    uint resultGIndexNew = ((plaqutte & 1) == 1) ?
          mt[uint2(mt[uint2(newA.x, plaq.x)], mt[uint2(plaq.y, plaq.z)])]
        : mt[uint2(mt[uint2(plaq.x, plaq.y)], mt[uint2(newA.y, plaq.z)])];

    return float2(etable[uint2(resultGIndexOld, 0)], etable[uint2(resultGIndexNew, 0)]);
}

//-------------------------------------
//Compare and exchange energy
//-------------------------------------
inline uint2 Exchange(uint2 oldA, uint2 newA, float2 energy2, uint iRandom, uint randomMaskY, RWTexture2D<float> rtable)
{
    if (energy2.x > energy2.y)
    {
        return newA;
    }

    float fRandom = rtable[uint2(iRandom >> 11, iRandom & randomMaskY)];
    if (fRandom < exp(energy2.x - energy2.y))
    {
        return newA;
    }
    return oldA;
}
