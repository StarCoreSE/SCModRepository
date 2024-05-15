#!/bin/bash

# Part of the UniversalUpload workflow, by Aristeas.
# Don't touch this unless stuff breaks, everything's automatic.

IFS=','; arrIN=($1); unset IFS;
find . -type f -name "*.sbmi" >> ./allModDatas.txt

MODIDARR=()

while read path; do
  while read sbmiLine; do
      if [[ $sbmiLine = \<Id\>* ]] ; then
          tmp=${sbmiLine#*>}
          modId=${tmp%<*}
          modPathTmp=${path%/*}
          modPath=${modPathTmp// /\`}
		  
		  for editedFile in "${arrIN[@]}"
		  do
			if [[ "./$editedFile" == "$modPathTmp"* ]] ; then
				MODIDARR+=(\{\"value\":$modId,\"path\":\"$modPath\"\})
				break
			fi
		  done
      fi
  done < "$path"
done < allModDatas.txt

delim=""
joined=""
for item in "${MODIDARR[@]}"; do
  joined="${joined}${delim}${item//\`/ }"
  delim=","
done
echo "matrix={\"include\":[$joined]}]"

echo > allModDatas.txt