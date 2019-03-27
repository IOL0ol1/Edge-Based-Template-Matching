Edge Based Template Matching     
==============================

### References   

[Edge Based Template Matching](https://www.codeproject.com/Articles/99457/Edge-Based-Template-Matching)    


### Note

The operation of establishing the gradient template can be completed simultaneously when the Canny algorithm is looking for the edge, and the original article inserts the intermediate calculation into it by implementing the Canny algorithm itself.     

Here is the implementation that is different.      
First use canny to find the edges, and then traverse the edges to create a gradient template

Use conditional compilation symbol "FAST" to ciew an unstable result     

### Preview   
![click to preview](preview.gif)

### Library   

Prism   
EmguCV     

### Other information     

[Prof. Dr. Carsten Steger(Halcon)](https://iuks.in.tum.de/members/steger/publications)            
[Some video and publications(At the bottom)](http://campar.in.tum.de/Main/AndreasHofhauser)            
