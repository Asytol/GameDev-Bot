#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>
#include <gtk/gtk.h>


#define DiscordCommandSize 32
#define ImageSavePath "GtkImageOutput.png"

static cairo_surface_t *surface = NULL;

//gcc $( pkg-config --cflags gtk4) -o GtkDrawingApp GtkDrawingApp.c $( pkg-config --libs gtk4)

static void clear_surface (void){
    cairo_t *cr;

    cr = cairo_create (surface);
    
    cairo_set_source_rgb(cr,1,1,1);
    cairo_paint(cr);

    cairo_destroy(cr);
}

static void resize_cb (GtkWidget *widget, int width, int height, gpointer data){
    if (surface){
        cairo_surface_destroy(surface);
        surface = NULL;
    }

    if (gtk_native_get_surface(gtk_widget_get_native(widget))){
        surface = cairo_image_surface_create(CAIRO_FORMAT_ARGB32,gtk_widget_get_width(widget),gtk_widget_get_height(widget));

        clear_surface();
    }
}

static void set_cb_size(int width, int height){
    if (surface){
        cairo_surface_destroy(surface);
        surface = NULL;
    }

    surface = cairo_image_surface_create(CAIRO_FORMAT_ARGB32,width,height);
    clear_surface();
}

static void draw_cb(GtkDrawingArea *drawing_area,cairo_t *cr, int width, int height, gpointer data){
    cairo_set_source_surface(cr,surface,0,0);
    cairo_paint(cr);
}

static void draw_brush(GtkWidget *widget, double x, double y, double red, double green, double blue){
    cairo_t *cr;
    
    cr = cairo_create(surface);
    cairo_set_source_rgb(cr,red,green,blue);

    cairo_rectangle(cr, x - 3, y - 3,6,6);
    cairo_fill(cr);

    cairo_destroy(cr);

    gtk_widget_queue_draw(widget);
}

static void pressed(GtkGestureClick *gesture, int n_press, double x, double y, GtkWidget *area){
    draw_brush(area, x, y,0,0,0);
    cairo_surface_write_to_png(surface,ImageSavePath);
    g_print("drew at %f %f",x,y);
}

static void close_window(void){
    if (surface){
        cairo_surface_destroy(surface);
    }
}

char inBuffer[DiscordCommandSize];
int p[3];


gboolean Callback(void* data){
    p[0] = dup(fileno(stdin));

    //write(p[1], "244:255:200:5:60:.", DiscordCommandSize);

    read(p[0],inBuffer,DiscordCommandSize);

    int values[5];

    char TempChar[100];
    char* inBufferIndex = inBuffer;
    if (inBuffer != ""){ 
        char* StartIndex = inBuffer;
        uint8_t length = 0;

        for (int i = 0; i < sizeof(values)/sizeof(values[0]); i++){
            StartIndex = inBufferIndex;
            length = 0;

            while(true){
                if (*inBufferIndex == '\0'){return TRUE;}
                if (*inBufferIndex == ':'){
                    inBufferIndex++;
                    break;
                }
                length++;
                inBufferIndex++;
            }
     
            if (length > 3){length = 3;}
            memcpy(TempChar,StartIndex,length);
            if (length < 3){TempChar[length] = '\0';}

            sscanf(TempChar,"%d",&values[i]);
        }
        g_print("red: %d, green: %d, blue: %d, x: %d, y: %d\n",values[0],values[1],values[2],values[3],values[4]);
        
        //Seg faulted for some reason due to one of these 2 lines? I'm gonna just not look into that.


        draw_brush((GtkWidget*)(gpointer*)data,values[3],values[4],(double)values[0]/255,(double)values[1]/255,(double)values[2]/255);
        
        cairo_surface_write_to_png(surface,ImageSavePath);
    }

    return TRUE;
}

static void activate(GtkApplication *app, gpointer data){
    GtkWidget *window;
    GtkWidget *frame;
    GtkWidget *drawing_area;
    GtkGesture *drag;
    GtkGesture *press;

    //window = gtk_application_window_new(app);
    //gtk_window_set_title(GTK_WINDOW(window),"BotDrawingWindow");

    //g_signal_connect(window, "destroy", G_CALLBACK(close_window),NULL);

    frame = gtk_frame_new(NULL);
    //gtk_window_set_child(GTK_WINDOW(window),frame);

    drawing_area = gtk_drawing_area_new();

    gtk_widget_set_size_request(drawing_area,64,64);

    gtk_frame_set_child(GTK_FRAME(frame),drawing_area);

    gtk_drawing_area_set_draw_func(GTK_DRAWING_AREA(drawing_area), draw_cb, NULL,NULL);

    set_cb_size(64,64);

    gpointer *Drawing_pointer;
    //List->data = drawing_area;
    Drawing_pointer = (void*)drawing_area;

    if (pipe(p) < 0){
        close(p[0]); close(p[1]);
        return;
    }

    g_timeout_add_seconds(0.4,Callback,Drawing_pointer);

    //gtk_window_present(GTK_WINDOW(window));
}

int main(int argc, char** argv){
    GtkApplication *app;
    int status;
    
    app = gtk_application_new("DiscordBot.DrawingSocket.Application",G_APPLICATION_DEFAULT_FLAGS);
    g_signal_connect(app,"activate",G_CALLBACK(activate),NULL);
    status = g_application_run(G_APPLICATION(app),argc,argv);
    g_object_unref(app);

    return status;
}