"""Independent positive-distance reciprocal audit against already byte-verified CELL coefficients."""
import numpy as np


def verify(lights,original,synthetic):
    maximum=0.0
    for row in original+synthetic:
        coefficients=row.get('coefficients')
        if coefficients is None:coefficients=next(r['attenuation'] for r in lights if r['cell']==row['cell'] and r['index']==row['index'] and r['kind']==2)
        coefficients=np.asarray(coefficients,dtype=np.float32).astype(float);squared=float(np.float32(row['distanceSquared']))
        denominator=np.dot(coefficients,[1,np.sqrt(squared),squared]);assert denominator>0 and squared>0
        expected=1/denominator;actual=float(np.float32(row['value']));error=abs(actual-expected)/max(1,abs(expected));maximum=max(maximum,error)
    assert len(original)==60 and len(synthetic)==48 and maximum<3e-7,(len(original),len(synthetic),maximum)
    assert any(r['value']>9000 for r in synthetic)
    return dict(original_coefficient_cases=len(original),synthetic_cases=len(synthetic),maximum_relative_error=maximum,denominator_cap=False,original_singular_behavior='unknown')
