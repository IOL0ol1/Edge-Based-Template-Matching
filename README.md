Edge Based Template Matching     
==============================

### References   

[Edge Based Template Matching](https://www.codeproject.com/Articles/99457/Edge-Based-Template-Matching)    


### Different    

The reference article implements the Canny algorithm itself.      
Inserts the operation that creates the gradient template into it.

The implementation here is different.

First use Canny algorithm to find the edges.     
Then traverse the edges to create a gradient template.
This's slow,but less code(use EmguCV's Canny)




### Note    

Use conditional compilation symbol "FAST" to view an faster but unstable result     

### Preview   
![click to preview](preview.gif)

### Library   

Prism   
EmguCV     

### Other information     

[Prof. Dr. Carsten Steger(Halcon)](https://iuks.in.tum.de/members/steger/publications)            
[Some video and publications(At the bottom)](http://campar.in.tum.de/Main/AndreasHofhauser)            
