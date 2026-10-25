# Optional offline checks

These tools are not required to open or play the Unity project.

To repeat the static/reference/geometry and numerical route checks in a Python environment:

```
python -m pip install -r Tools/requirements.txt
python Tools/ValidateOffline.py
```

The script writes `Documentation/StaticValidation.json`. It uses tree-sitter to parse C# syntax; it does not compile Unity code. The Unity editor validation menu and device playtest remain necessary.
